using System.Text;
using AutoVerdict.Application.AI;
using AutoVerdict.Application.Storage;
using AutoVerdict.Contracts.Listing;
using AutoVerdict.Contracts.Messages;
using AutoVerdict.Infrastructure.AI;
using AutoVerdict.ProcessingService.Crawler;
using AutoVerdict.ProcessingService.Parsing;
using Microsoft.Extensions.Options;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class CarCheckAnalysisPipeline(
    IDocumentStorageClient storage,
    OtomotoListingParser listingParser,
    DomainRateLimiter rateLimiter,
    FactExtractionStage factExtractionStage,
    RiskAnalysisStage riskAnalysisStage,
    ReportGenerationStage reportGenerationStage,
    ReportValidator reportValidator,
    ReportRepairStage reportRepairStage,
    AiStageRunner stageRunner,
    IOptions<AiPipelineOptions> aiPipelineOptions,
    ILogger<CarCheckAnalysisPipeline> logger)
{
    private const string AnalysisFileName = "ai-analysis-result.md";
    private readonly AiPipelineOptions _aiPipelineOptions = aiPipelineOptions.Value;

    public async Task<string> ExecuteAsync(
        CarCheckRequestedMessage message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running staged AI analysis for check {CheckId}.", message.CheckId);

        var userImages = await DownloadUserImagesAsync(message.UserImageKeys, cancellationToken);
        var (screenshotBytes, crawledListing) = await CrawlListingAsync(message, cancellationToken);
        var evidence = new EvidenceBundle(
            message.CheckId,
            message.Description,
            message.ListingUrl,
            crawledListing,
            userImages ?? [],
            screenshotBytes is { Length: > 0 }
                ? new UserImageContent(screenshotBytes, "image/png")
                : null);

        var budget = new AiBudgetTracker(_aiPipelineOptions.HardBudgetEur);
        var facts = await factExtractionStage.ExecuteAsync(evidence, budget, cancellationToken);
        var risks = await riskAnalysisStage.ExecuteAsync(evidence, facts, budget, cancellationToken);

        var useOpusForReport = ShouldUseOpusForReport(risks, budget);
        var report = await reportGenerationStage.ExecuteAsync(
            evidence,
            facts,
            risks,
            budget,
            useOpusForReport,
            cancellationToken);

        var validation = reportValidator.Validate(report.Markdown);
        var finalMarkdown = report.Markdown;
        if (!validation.IsValid)
        {
            logger.LogWarning(
                "Report validation failed for check {CheckId}: {Errors}. Attempting repair.",
                message.CheckId,
                string.Join("; ", validation.Errors));

            finalMarkdown = await reportRepairStage.ExecuteAsync(
                message.CheckId,
                report.Markdown,
                validation,
                budget,
                cancellationToken);

            validation = reportValidator.Validate(finalMarkdown);
            if (!validation.IsValid)
                throw new InvalidOperationException(
                    "AI report failed validation after repair: " + string.Join("; ", validation.Errors));
        }

        logger.LogInformation(
            "Staged AI analysis complete for check {CheckId}; estimated AI spend is {CostEur} EUR.",
            message.CheckId,
            budget.SpentEur);

        return await SaveAnalysisAsync(message.CheckId, finalMarkdown, cancellationToken);
    }

    private bool ShouldUseOpusForReport(RiskAnalysisResult risks, AiBudgetTracker budget)
    {
        var opusStage = _aiPipelineOptions.GetStage("OpusReview", "claude-opus-4-1", 8000);
        if (!opusStage.Enabled || !risks.NeedsEscalation)
            return false;

        var estimatedCost = stageRunner.EstimateMaxCostEur(opusStage.Model, 6000, opusStage.MaxTokens);
        if (budget.CanSpend(estimatedCost))
        {
            logger.LogInformation(
                "Escalating report generation to Opus-compatible model {Model}: {Reason}",
                opusStage.Model,
                risks.EscalationReason);
            return true;
        }

        logger.LogInformation(
            "Skipping Opus escalation for budget reasons. Estimated next stage cost {EstimatedCost}; spent {Spent}; hard budget {Budget}.",
            estimatedCost,
            budget.SpentEur,
            budget.HardBudgetEur);
        return false;
    }

    private async Task<(byte[]? ScreenshotBytes, CarListingSnapshot? Listing)> CrawlListingAsync(
        CarCheckRequestedMessage message,
        CancellationToken cancellationToken)
    {
        if (message.ListingUrl is null || !OtomotoListingParser.IsSupported(message.ListingUrl))
            return (null, null);

        var domain = new Uri(message.ListingUrl).Host;
        var rateLimitAcquired = false;

        try
        {
            await rateLimiter.WaitAsync(domain, cancellationToken);
            rateLimitAcquired = true;

            logger.LogInformation("Crawling listing {Url} for check {CheckId}.", message.ListingUrl, message.CheckId);

            var screenshotKey = $"{message.CheckId}/listing-screenshot.png";
            var parseResult = await listingParser.ParseAsync(
                message.CheckId, message.ListingUrl, screenshotKey, cancellationToken);

            await using var stream = new MemoryStream(parseResult.ScreenshotBytes);
            await storage.UploadAsync(screenshotKey, stream, parseResult.ScreenshotContentType, cancellationToken);

            if (parseResult.DetectedBlockOrCaptcha)
                logger.LogWarning("CAPTCHA/block detected for check {CheckId} — crawled data may be incomplete.", message.CheckId);
            else
                logger.LogInformation("Listing crawled successfully for check {CheckId}.", message.CheckId);

            return (parseResult.ScreenshotBytes, parseResult.Listing);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Listing crawl failed for check {CheckId}; proceeding without crawled data.", message.CheckId);
            return (null, null);
        }
        finally
        {
            if (rateLimitAcquired)
                rateLimiter.Release(domain);
        }
    }

    private async Task<string> SaveAnalysisAsync(Guid checkId, string markdown, CancellationToken cancellationToken)
    {
        var key = $"{checkId}/{AnalysisFileName}";
        var bytes = Encoding.UTF8.GetBytes(markdown);
        await using var stream = new MemoryStream(bytes);
        await storage.UploadAsync(key, stream, "text/markdown; charset=utf-8", cancellationToken);
        logger.LogInformation("Analysis for check {CheckId} saved to {Key}.", checkId, key);
        return key;
    }

    private async Task<IReadOnlyList<UserImageContent>?> DownloadUserImagesAsync(
        string[]? keys,
        CancellationToken cancellationToken)
    {
        if (keys is not { Length: > 0 }) return null;

        var images = new List<UserImageContent>(keys.Length);
        foreach (var key in keys)
        {
            try
            {
                var (bytes, contentType) = await storage.DownloadAsync(key, cancellationToken);
                images.Add(new UserImageContent(bytes, contentType));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to download user image {Key}; skipping.", key);
            }
        }
        return images.Count > 0 ? images : null;
    }
}

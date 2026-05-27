using System.Text;
using AutoVerdict.Application.AI;
using AutoVerdict.Application.Storage;
using AutoVerdict.Contracts.Listing;
using AutoVerdict.Contracts.Messages;
using AutoVerdict.ProcessingService.Crawler;
using AutoVerdict.ProcessingService.Parsing;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class CarCheckAnalysisPipeline(
    IAiAnalysisProvider aiProvider,
    IDocumentStorageClient storage,
    OtomotoListingParser listingParser,
    DomainRateLimiter rateLimiter,
    ILogger<CarCheckAnalysisPipeline> logger)
{
    private const string AnalysisFileName = "ai-analysis-result.md";

    public async Task<string> ExecuteAsync(
        CarCheckRequestedMessage message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running AI analysis for check {CheckId}.", message.CheckId);

        var userImages = await DownloadUserImagesAsync(message.UserImageKeys, cancellationToken);
        var (screenshotBytes, crawledListing) = await CrawlListingAsync(message, cancellationToken);

        var request = new AiAnalysisRequest(
            message.CheckId,
            message.Description,
            message.ListingUrl,
            userImages,
            screenshotBytes,
            "image/png",
            crawledListing);

        var result = await aiProvider.AnalyzeAsync(request, cancellationToken);

        logger.LogInformation(
            "AI analysis complete for check {CheckId}: provider={Provider}, model={Model}, tokens={Input}+{Output}.",
            message.CheckId, result.ProviderName, result.ModelName,
            result.InputTokens, result.OutputTokens);

        return await SaveAnalysisAsync(message.CheckId, result.MarkdownText, cancellationToken);
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

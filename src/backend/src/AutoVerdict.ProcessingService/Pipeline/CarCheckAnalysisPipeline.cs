using System.Text;
using AutoVerdict.Application.AI;
using AutoVerdict.Application.Storage;
using AutoVerdict.Contracts.Listing;
using AutoVerdict.Contracts.Messages;
using AutoVerdict.ProcessingService.Parsing;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class CarCheckAnalysisPipeline(
    IAiAnalysisProvider aiProvider,
    IDocumentStorageClient storage,
    OtomotoListingParser listingParser,
    ILogger<CarCheckAnalysisPipeline> logger)
{
    private const string AnalysisFileName = "ai-analysis-result.md";

    public async Task<string> ExecuteAsync(
        CarCheckRequestedMessage message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running AI analysis for check {CheckId}.", message.CheckId);

        IReadOnlyList<UserImageContent>? userImages = null;
        if (message.UserImageKeys is { Length: > 0 })
            userImages = await DownloadUserImagesAsync(message.UserImageKeys, cancellationToken);

        byte[]? screenshotBytes = null;
        CarListingSnapshot? crawledListing = null;

        if (message.ListingUrl is not null && OtomotoListingParser.IsSupported(message.ListingUrl))
        {
            var screenshotStorageKey = $"{message.CheckId}/listing-screenshot.png";
            try
            {
                logger.LogInformation("Crawling listing {Url} for check {CheckId}.", message.ListingUrl, message.CheckId);

                var parseResult = await listingParser.ParseAsync(
                    message.CheckId, message.ListingUrl, screenshotStorageKey, cancellationToken);

                await using var screenshotStream = new MemoryStream(parseResult.ScreenshotBytes);
                await storage.UploadAsync(screenshotStorageKey, screenshotStream, parseResult.ScreenshotContentType, cancellationToken);

                screenshotBytes = parseResult.ScreenshotBytes;
                crawledListing = parseResult.Listing;

                if (parseResult.DetectedBlockOrCaptcha)
                    logger.LogWarning("CAPTCHA/block detected for check {CheckId} — crawled data may be incomplete.", message.CheckId);
                else
                    logger.LogInformation("Listing crawled successfully for check {CheckId}.", message.CheckId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Listing crawl failed for check {CheckId}; proceeding without crawled data.", message.CheckId);
            }
        }

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

        var storageKey = $"{message.CheckId}/{AnalysisFileName}";
        var markdownBytes = Encoding.UTF8.GetBytes(result.MarkdownText);
        await using var stream = new MemoryStream(markdownBytes);
        await storage.UploadAsync(storageKey, stream, "text/markdown; charset=utf-8", cancellationToken);

        logger.LogInformation("Analysis for check {CheckId} saved to {Key}.", message.CheckId, storageKey);

        return storageKey;
    }

    private async Task<IReadOnlyList<UserImageContent>> DownloadUserImagesAsync(
        string[] keys,
        CancellationToken cancellationToken)
    {
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
        return images;
    }
}

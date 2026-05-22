using AutoVerdict.Application.AI;
using AutoVerdict.Application.Storage;
using AutoVerdict.Contracts.Messages;
using AutoVerdict.ProcessingService.Parsing;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class CarCheckAnalysisPipeline(
    IDocumentStorageClient storage,
    ICarListingParser listingParser,
    IAiAnalysisProvider aiProvider,
    ILogger<CarCheckAnalysisPipeline> logger)
{
    public async Task<AiAnalysisResult> ExecuteAsync(
        CarCheckRequestedMessage message,
        CancellationToken cancellationToken = default)
    {
        var screenshotStorageKey = $"checks/{message.UserId}/{message.CheckId}/otomoto-full-page.png";

        logger.LogInformation(
            "Parsing listing {ListingUrl} for check {CheckId}.",
            message.ListingUrl, message.CheckId);

        var parsed = await listingParser.ParseAsync(
            message.CheckId,
            message.ListingUrl,
            screenshotStorageKey,
            cancellationToken);

        await using var screenshotStream = new MemoryStream(parsed.ScreenshotBytes);
        await storage.UploadAsync(
            screenshotStorageKey,
            screenshotStream,
            parsed.ScreenshotContentType,
            cancellationToken);

        logger.LogInformation(
            "Uploaded full-page listing screenshot {StorageKey} ({Bytes} bytes).",
            screenshotStorageKey, parsed.ScreenshotBytes.Length);

        var request = new AiAnalysisRequest(
            message.CheckId,
            parsed.Listing,
            parsed.ScreenshotBytes,
            parsed.ScreenshotContentType);

        AiAnalysisResult result = await aiProvider.AnalyzeAsync(request, cancellationToken);

        logger.LogInformation(
            "AI analysis complete for check {CheckId}: provider={Provider}, model={Model}, tokens={Input}+{Output}.",
            message.CheckId, result.ProviderName, result.ModelName,
            result.InputTokens, result.OutputTokens);

        return result;
    }
}

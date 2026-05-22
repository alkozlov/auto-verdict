using AutoVerdict.Application.AI;
using AutoVerdict.Application.Storage;
using AutoVerdict.Contracts.Messages;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class CarCheckAnalysisPipeline(
    IDocumentStorageClient storage,
    IAiAnalysisProvider aiProvider,
    ILogger<CarCheckAnalysisPipeline> logger)
{
    public async Task<AiAnalysisResult> ExecuteAsync(
        CarCheckRequestedMessage message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Downloading document {StorageKey} for check {CheckId}.",
            message.DocumentStorageKey, message.CheckId);

        (byte[] documentBytes, string contentType) =
            await storage.DownloadAsync(message.DocumentStorageKey, cancellationToken);

        logger.LogInformation(
            "Document downloaded ({Bytes} bytes, {ContentType}). Invoking AI analysis for check {CheckId}.",
            documentBytes.Length, contentType, message.CheckId);

        var request = new AiAnalysisRequest(
            message.CheckId,
            message.VehicleIdentifier,
            documentBytes,
            contentType);

        AiAnalysisResult result = await aiProvider.AnalyzeAsync(request, cancellationToken);

        logger.LogInformation(
            "AI analysis complete for check {CheckId}: provider={Provider}, model={Model}, tokens={Input}+{Output}.",
            message.CheckId, result.ProviderName, result.ModelName,
            result.InputTokens, result.OutputTokens);

        return result;
    }
}

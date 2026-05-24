using AutoVerdict.Application.AI;
using AutoVerdict.Contracts.Messages;
using AutoVerdict.ProcessingService.Parsing;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class CarCheckAnalysisPipeline(
    IAiAnalysisProvider aiProvider,
    ILogger<CarCheckAnalysisPipeline> logger)
{
    public async Task<AiAnalysisResult> ExecuteAsync(
        CarCheckRequestedMessage message,
        ListingParseResult parsed,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running AI analysis for check {CheckId}.", message.CheckId);

        var request = new AiAnalysisRequest(
            message.CheckId,
            parsed.Listing,
            parsed.ScreenshotBytes,
            parsed.ScreenshotContentType);

        var result = await aiProvider.AnalyzeAsync(request, cancellationToken);

        logger.LogInformation(
            "AI analysis complete for check {CheckId}: provider={Provider}, model={Model}, tokens={Input}+{Output}.",
            message.CheckId, result.ProviderName, result.ModelName,
            result.InputTokens, result.OutputTokens);

        return result;
    }
}

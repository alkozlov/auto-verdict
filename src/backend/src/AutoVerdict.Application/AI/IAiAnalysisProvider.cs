namespace AutoVerdict.Application.AI;

public interface IAiAnalysisProvider
{
    Task<AiAnalysisResult> AnalyzeAsync(AiAnalysisRequest request, CancellationToken cancellationToken = default);
}

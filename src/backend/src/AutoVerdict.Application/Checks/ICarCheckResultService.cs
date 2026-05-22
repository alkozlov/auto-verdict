using AutoVerdict.Application.AI;

namespace AutoVerdict.Application.Checks;

public interface ICarCheckResultService
{
    Task RecordSuccessAsync(Guid checkId, AiAnalysisResult result, CancellationToken cancellationToken = default);
    Task RecordFailureAsync(Guid checkId, string reason, CancellationToken cancellationToken = default);
}

namespace AutoVerdict.Application.Checks;

public interface ICarCheckResultService
{
    Task RecordSuccessAsync(Guid checkId, string analysisStorageKey, CancellationToken cancellationToken = default);
    Task RecordFailureAsync(Guid checkId, string reason, CancellationToken cancellationToken = default);
}

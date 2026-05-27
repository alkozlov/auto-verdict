namespace AutoVerdict.Contracts.Messages;

public sealed record CarCheckCompletedMessage(
    Guid CheckId,
    Guid UserId,
    string AnalysisStorageKey,
    DateTimeOffset CompletedAt);

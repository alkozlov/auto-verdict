namespace AutoVerdict.Contracts.Messages;

public sealed record CarCheckFailedMessage(
    Guid CheckId,
    Guid UserId,
    string Reason,
    DateTimeOffset FailedAt);

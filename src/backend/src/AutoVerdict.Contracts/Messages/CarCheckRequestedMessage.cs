namespace AutoVerdict.Contracts.Messages;

public sealed record CarCheckRequestedMessage(
    Guid CheckId,
    Guid UserId,
    string VehicleIdentifier,
    string DocumentStorageKey,
    DateTimeOffset RequestedAt);

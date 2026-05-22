using AutoVerdict.Contracts.Report;

namespace AutoVerdict.Contracts.Messages;

public sealed record CarCheckCompletedMessage(
    Guid CheckId,
    Guid UserId,
    VehicleReport Report,
    DateTimeOffset CompletedAt);

using AutoVerdict.Contracts.Enums;
using AutoVerdict.Contracts.Report;

namespace AutoVerdict.Contracts.Dtos;

public sealed record CarCheckResponse(
    Guid CheckId,
    string VehicleIdentifier,
    CarCheckStatus Status,
    VehicleReport? Report,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

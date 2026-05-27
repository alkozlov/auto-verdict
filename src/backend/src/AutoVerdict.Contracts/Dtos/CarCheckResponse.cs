using AutoVerdict.Contracts.Enums;

namespace AutoVerdict.Contracts.Dtos;

public sealed record CarCheckResponse(
    Guid CheckId,
    string? Title,
    string? ListingUrl,
    CarCheckStatus Status,
    string? Report,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

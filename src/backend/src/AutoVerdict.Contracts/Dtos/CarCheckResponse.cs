using AutoVerdict.Contracts.Enums;
using AutoVerdict.Contracts.Report;

namespace AutoVerdict.Contracts.Dtos;

public sealed record CarCheckResponse(
    Guid CheckId,
    string ListingUrl,
    string? Title,
    string? Make,
    string? Model,
    int? Year,
    int? MileageKm,
    decimal? Price,
    string? Currency,
    CarCheckStatus Status,
    VehicleReport? Report,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

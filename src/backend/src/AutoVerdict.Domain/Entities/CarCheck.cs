using AutoVerdict.Contracts.Enums;

namespace AutoVerdict.Domain.Entities;

public sealed class CarCheck
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string VehicleIdentifier { get; set; } = null!;
    public string ListingUrl { get; set; } = null!;
    public string? DocumentStorageKey { get; set; }
    public string? Title { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public int? MileageKm { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public string? ScreenshotStorageKey { get; set; }
    public CarCheckStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public CarReport? Report { get; set; }
    public List<AiRequest> AiRequests { get; set; } = [];
}

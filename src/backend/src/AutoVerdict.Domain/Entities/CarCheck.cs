using AutoVerdict.Contracts.Enums;

namespace AutoVerdict.Domain.Entities;

public sealed class CarCheck
{
    public int Id { get; set; }
    public Guid CheckId { get; set; }
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public string Description { get; set; } = null!;
    public string? ListingUrl { get; set; }
    public string? UserImageKeysJson { get; set; }
    public string? AnalysisStorageKey { get; set; }
    public CarCheckStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}

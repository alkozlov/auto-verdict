using AutoVerdict.Contracts.Enums;

namespace AutoVerdict.Domain.Entities;

public sealed class CarCheck
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string VehicleIdentifier { get; set; } = null!;
    public string DocumentStorageKey { get; set; } = null!;
    public CarCheckStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public CarReport? Report { get; set; }
    public List<AiRequest> AiRequests { get; set; } = [];
}

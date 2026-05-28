namespace AutoVerdict.Domain.Entities;

public sealed class PaymentOrder
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PackageKey { get; set; } = null!;
    public int CreditsGranted { get; set; }
    public string ExternalOrderId { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}

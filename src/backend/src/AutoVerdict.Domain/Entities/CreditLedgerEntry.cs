namespace AutoVerdict.Domain.Entities;

public sealed class CreditLedgerEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int Amount { get; set; }
    public string Reason { get; set; } = null!;
    public Guid? ReferenceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}

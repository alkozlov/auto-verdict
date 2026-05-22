namespace AutoVerdict.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string? DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<ExternalAuthAccount> ExternalAuthAccounts { get; set; } = [];
    public List<CreditLedgerEntry> CreditLedger { get; set; } = [];
    public UserCredits? Credits { get; set; }
}

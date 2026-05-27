namespace AutoVerdict.Domain.Entities;

public sealed class UserCredits
{
    public Guid UserId { get; set; }
    public int Balance { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}

namespace AutoVerdict.Domain.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

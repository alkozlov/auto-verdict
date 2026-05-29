namespace AutoVerdict.Domain.Entities;

public sealed class AiRun
{
    public int Id { get; set; }
    public Guid CheckId { get; set; }
    public string Stage { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string PromptVersion { get; set; } = null!;
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public decimal EstimatedCostEur { get; set; }
    public long DurationMs { get; set; }
    public string Status { get; set; } = null!;
    public string? ErrorMessage { get; set; }
    public string? EscalationReason { get; set; }
    public string? ValidationWarningsJson { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

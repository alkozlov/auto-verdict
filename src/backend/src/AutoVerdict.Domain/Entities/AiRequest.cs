namespace AutoVerdict.Domain.Entities;

public sealed class AiRequest
{
    public Guid Id { get; set; }
    public Guid CarCheckId { get; set; }
    public string ProviderName { get; set; } = null!;
    public string ModelName { get; set; } = null!;
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public CarCheck CarCheck { get; set; } = null!;
}

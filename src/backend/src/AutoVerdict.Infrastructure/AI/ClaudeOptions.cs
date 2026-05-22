namespace AutoVerdict.Infrastructure.AI;

public sealed class ClaudeOptions
{
    public const string SectionName = "Claude";

    public string ApiKey { get; set; } = null!;
    public string Model { get; set; } = "claude-opus-4-7";
    public int MaxTokens { get; set; } = 16000;
}

namespace AutoVerdict.Infrastructure.AI;

public sealed class ClaudeOptions
{
    public const string SectionName = "Claude";

    public string ApiKey { get; set; } = null!;
    public string Model { get; set; } = "claude-haiku-4-5";
    public int MaxTokens { get; set; } = 16000;
}

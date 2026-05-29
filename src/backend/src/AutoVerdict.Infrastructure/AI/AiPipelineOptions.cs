namespace AutoVerdict.Infrastructure.AI;

public sealed class AiPipelineOptions
{
    public const string SectionName = "AiPipeline";

    public bool Enabled { get; set; } = true;
    public string Currency { get; set; } = "EUR";
    public decimal DefaultBudgetEur { get; set; } = 0.70m;
    public decimal ComplexBudgetEur { get; set; } = 1.50m;
    public decimal HardBudgetEur { get; set; } = 2.00m;
    public Dictionary<string, AiStageOptions> Stages { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public AiStageOptions GetStage(string stage, string fallbackModel, int fallbackMaxTokens)
    {
        if (Stages.TryGetValue(stage, out var options))
            return options;

        return new AiStageOptions
        {
            Model = fallbackModel,
            MaxTokens = fallbackMaxTokens,
            Enabled = true,
        };
    }
}

public sealed class AiStageOptions
{
    public string Model { get; set; } = "claude-sonnet-4-6";
    public int MaxTokens { get; set; } = 4000;
    public bool Enabled { get; set; } = true;
}

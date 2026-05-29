namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class AiBudgetTracker(decimal hardBudgetEur)
{
    public decimal HardBudgetEur { get; } = hardBudgetEur;
    public decimal SpentEur { get; private set; }

    public void Add(decimal costEur) => SpentEur += Math.Max(0, costEur);

    public bool CanSpend(decimal estimatedCostEur) =>
        SpentEur + Math.Max(0, estimatedCostEur) <= HardBudgetEur;
}

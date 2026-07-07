using AutoVerdict.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoVerdict.ProcessingService.Pipeline;

public static class AiSpend
{
    /// <summary>
    /// Total estimated cost of all persisted AI runs for a check (across every
    /// prior attempt). Summed as double because SQLite (used in tests) cannot
    /// aggregate decimals server-side; sub-cent precision loss is irrelevant.
    /// </summary>
    public static async Task<decimal> SumPriorForCheckAsync(
        AppDbContext db, Guid checkId, CancellationToken ct)
    {
        var sum = await db.AiRuns
            .Where(r => r.CheckId == checkId)
            .Select(r => (double)r.EstimatedCostEur)
            .SumAsync(ct);
        return (decimal)sum;
    }
}

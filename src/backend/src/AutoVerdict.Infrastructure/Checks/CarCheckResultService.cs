using AutoVerdict.Application.AI;
using AutoVerdict.Application.Checks;
using AutoVerdict.Contracts.Enums;
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoVerdict.Infrastructure.Checks;

public sealed class CarCheckResultService(AppDbContext db) : ICarCheckResultService
{
    public async Task RecordSuccessAsync(
        Guid checkId,
        AiAnalysisResult result,
        CancellationToken cancellationToken = default)
    {
        var check = await db.CarChecks.FindAsync([checkId], cancellationToken)
            ?? throw new InvalidOperationException($"CarCheck {checkId} not found.");

        var now = DateTimeOffset.UtcNow;

        db.CarReports.Add(new CarReport
        {
            Id = Guid.NewGuid(),
            CarCheckId = checkId,
            ReportData = result.Report,
            CreatedAt = now,
        });

        db.AiRequests.Add(new AiRequest
        {
            Id = Guid.NewGuid(),
            CarCheckId = checkId,
            ProviderName = result.ProviderName,
            ModelName = result.ModelName,
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            CreatedAt = now,
        });

        check.Status = CarCheckStatus.Completed;
        check.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordFailureAsync(
        Guid checkId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await db.CarChecks
            .Where(c => c.Id == checkId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(c => c.Status, CarCheckStatus.Failed)
                       .SetProperty(c => c.FailureReason, reason)
                       .SetProperty(c => c.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken);
    }
}

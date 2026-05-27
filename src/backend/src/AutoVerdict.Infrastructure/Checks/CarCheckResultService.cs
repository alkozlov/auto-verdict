using AutoVerdict.Application.Checks;
using AutoVerdict.Contracts.Configuration;
using AutoVerdict.Contracts.Enums;
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutoVerdict.Infrastructure.Checks;

public sealed class CarCheckResultService(AppDbContext db, IOptions<WhitelistOptions> whitelist) : ICarCheckResultService
{
    public async Task RecordSuccessAsync(
        Guid checkId,
        string analysisStorageKey,
        CancellationToken cancellationToken = default)
    {
        var check = await db.CarChecks
            .FirstOrDefaultAsync(c => c.CheckId == checkId, cancellationToken);
        if (check is null) return;

        var now = DateTimeOffset.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var userEmail = await db.Users
            .Where(u => u.Id == check.UserId)
            .Select(u => u.Email)
            .SingleOrDefaultAsync(cancellationToken);

        if (!whitelist.Value.Contains(userEmail ?? ""))
        {
            int rows = await db.Database.ExecuteSqlAsync(
                $"""
                UPDATE user_credits
                SET "Balance" = "Balance" - 1, "UpdatedAt" = NOW()
                WHERE "UserId" = {check.UserId} AND "Balance" >= 1
                """,
                cancellationToken);

            if (rows == 0)
                throw new InsufficientCreditsException();

            db.CreditLedgerEntries.Add(new CreditLedgerEntry
            {
                Id = Guid.NewGuid(),
                UserId = check.UserId,
                Amount = -1,
                Reason = "car_check_completed",
                ReferenceId = checkId,
                CreatedAt = now,
            });
        }

        check.AnalysisStorageKey = analysisStorageKey;
        check.Status = CarCheckStatus.Completed;
        check.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task RecordFailureAsync(
        Guid checkId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var check = await db.CarChecks
            .FirstOrDefaultAsync(c => c.CheckId == checkId, cancellationToken);
        if (check is null) return;

        var now = DateTimeOffset.UtcNow;
        check.Status = CarCheckStatus.Failed;
        check.FailureReason = reason;
        check.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
    }
}

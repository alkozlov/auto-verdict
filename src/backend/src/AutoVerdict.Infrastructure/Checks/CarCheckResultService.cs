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
        string analysisStorageKey,
        CancellationToken cancellationToken = default)
    {
        var check = await db.CarChecks
            .FirstOrDefaultAsync(c => c.CheckId == checkId, cancellationToken);
        if (check is null) return;

        check.AnalysisStorageKey = analysisStorageKey;
        check.Status = CarCheckStatus.Completed;
        check.FailureReason = null;
        check.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
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
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        check.Status = CarCheckStatus.Failed;
        check.FailureReason = reason;
        check.UpdatedAt = now;

        // Ledger-driven idempotent refund: only if this check reserved a credit
        // and was never refunded. Checks without a reservation (whitelisted
        // users, checks created before reservations existed) refund nothing.
        bool reserved = await db.CreditLedgerEntries
            .AnyAsync(e => e.ReferenceId == checkId && e.Reason == "car_check_reserved", cancellationToken);
        bool refunded = await db.CreditLedgerEntries
            .AnyAsync(e => e.ReferenceId == checkId && e.Reason == "car_check_refunded", cancellationToken);

        if (reserved && !refunded)
        {
            await db.UserCredits
                .Where(c => c.UserId == check.UserId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Balance, c => c.Balance + 1)
                    .SetProperty(c => c.UpdatedAt, now), cancellationToken);

            db.CreditLedgerEntries.Add(new CreditLedgerEntry
            {
                Id = Guid.NewGuid(),
                UserId = check.UserId,
                Amount = 1,
                Reason = "car_check_refunded",
                ReferenceId = checkId,
                CreatedAt = now,
            });
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
        {
            // A concurrent terminal-failure call won the refund race; the
            // rolled-back transaction discards this duplicate refund AND this
            // call's status write — the winner already wrote both.
        }
    }
}

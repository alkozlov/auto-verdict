using AutoVerdict.Application.AI;
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
        AiAnalysisResult result,
        CancellationToken cancellationToken = default)
    {
        var check = await db.CarChecks.FindAsync([checkId], cancellationToken);
        if (check is null)
        {
            // Orphaned message — no car_check row was ever created (e.g. direct NATS publish in tests).
            // Ack the message rather than retrying forever.
            return;
        }

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

        var vehicleIdentifier = result.Listing.Title
            ?? $"{result.Listing.Make} {result.Listing.Model}".Trim();

        check.VehicleIdentifier = string.IsNullOrWhiteSpace(vehicleIdentifier)
            ? result.Listing.ListingUrl
            : vehicleIdentifier;
        check.ListingUrl = result.Listing.ListingUrl;
        check.DocumentStorageKey = result.Listing.ScreenshotStorageKey;
        check.Title = result.Listing.Title;
        check.Make = result.Listing.Make;
        check.Model = result.Listing.Model;
        check.Year = result.Listing.Year;
        check.MileageKm = result.Listing.MileageKm;
        check.Price = result.Listing.Price;
        check.ScreenshotStorageKey = result.Listing.ScreenshotStorageKey;

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
        await tx.CommitAsync(cancellationToken);
    }

    public async Task RecordFailureAsync(
        Guid checkId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var check = await db.CarChecks.FindAsync([checkId], cancellationToken);
        if (check is null) return; // Orphaned message — no car_check row exists.

        var now = DateTimeOffset.UtcNow;
        check.Status = CarCheckStatus.Failed;
        check.FailureReason = reason;
        check.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
    }
}

using System.Text.Json;
using AutoVerdict.Application.Checks;
using AutoVerdict.Contracts.Configuration;
using AutoVerdict.Contracts.Enums;
using AutoVerdict.Contracts.Messages;
using AutoVerdict.Contracts.Reports;
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutoVerdict.Infrastructure.Checks;

public sealed class CarCheckService(AppDbContext db, IOptions<WhitelistOptions> whitelist) : ICarCheckService
{
    public async Task<CarCheck> CreateAsync(
        Guid userId,
        Guid checkId,
        string description,
        string? listingUrl,
        string reportLocale,
        string[] userImageKeys,
        CancellationToken cancellationToken = default)
    {
        var language = ReportLanguage.Resolve(reportLocale);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var userEmail = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .SingleOrDefaultAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (!whitelist.Value.Contains(userEmail ?? ""))
        {
            // Reserve the credit atomically at submission; refunded on terminal failure.
            int rows = await db.UserCredits
                .Where(c => c.UserId == userId && c.Balance >= 1)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Balance, c => c.Balance - 1)
                    .SetProperty(c => c.UpdatedAt, now), cancellationToken);
            if (rows == 0)
                throw new InsufficientCreditsException();

            db.CreditLedgerEntries.Add(new CreditLedgerEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = -1,
                Reason = "car_check_reserved",
                ReferenceId = checkId,
                CreatedAt = now,
            });
        }

        var title = description.Length <= 120 ? description.Trim() : description[..120].Trim() + "…";

        var check = new CarCheck
        {
            CheckId = checkId,
            UserId = userId,
            Title = title,
            Description = description,
            ListingUrl = listingUrl,
            UserImageKeysJson = userImageKeys.Length > 0 ? JsonSerializer.Serialize(userImageKeys) : null,
            Status = CarCheckStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.CarChecks.Add(check);

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Subject = NatsSubjects.CarCheckRequested,
            Payload = JsonSerializer.Serialize(new CarCheckRequestedMessage(
                checkId, userId, description, listingUrl, now, language.Locale, userImageKeys)),
            CreatedAt = now,
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return check;
    }
}

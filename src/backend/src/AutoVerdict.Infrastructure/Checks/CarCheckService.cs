using System.Text.Json;
using AutoVerdict.Application.Checks;
using AutoVerdict.Contracts.Configuration;
using AutoVerdict.Contracts.Enums;
using AutoVerdict.Contracts.Messages;
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoVerdict.Infrastructure.Checks;

public sealed class CarCheckService(AppDbContext db) : ICarCheckService
{
    public async Task<CarCheck> CreateAsync(
        Guid userId,
        string listingUrl,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        bool hasAvailableCredit = await db.UserCredits
            .AnyAsync(c => c.UserId == userId && c.Balance >= 1, cancellationToken);
        if (!hasAvailableCredit)
            throw new InsufficientCreditsException();

        var checkId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var check = new CarCheck
        {
            Id = checkId,
            UserId = userId,
            VehicleIdentifier = listingUrl,
            ListingUrl = listingUrl,
            Status = CarCheckStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.CarChecks.Add(check);

        var outboxPayload = JsonSerializer.Serialize(new CarCheckRequestedMessage(
            checkId, userId, listingUrl, now));

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Subject = NatsSubjects.CarCheckRequested,
            Payload = outboxPayload,
            CreatedAt = now,
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return check;
    }
}

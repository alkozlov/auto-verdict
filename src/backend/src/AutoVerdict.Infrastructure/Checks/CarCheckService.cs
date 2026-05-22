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
        string vehicleIdentifier,
        string documentStorageKey,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        // Atomically deduct one credit — WHERE guard prevents going below zero.
        int rows = await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE user_credits
            SET "Balance" = "Balance" - 1, "UpdatedAt" = NOW()
            WHERE "UserId" = {userId} AND "Balance" >= 1
            """,
            cancellationToken);

        if (rows == 0)
            throw new InsufficientCreditsException();

        var checkId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.CreditLedgerEntries.Add(new CreditLedgerEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = -1,
            Reason = "car_check",
            ReferenceId = checkId,
            CreatedAt = now,
        });

        var check = new CarCheck
        {
            Id = checkId,
            UserId = userId,
            VehicleIdentifier = vehicleIdentifier,
            DocumentStorageKey = documentStorageKey,
            Status = CarCheckStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.CarChecks.Add(check);

        var outboxPayload = JsonSerializer.Serialize(new CarCheckRequestedMessage(
            checkId, userId, vehicleIdentifier, documentStorageKey, now));

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

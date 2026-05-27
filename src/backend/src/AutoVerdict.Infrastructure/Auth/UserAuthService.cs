using AutoVerdict.Application.Auth;
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoVerdict.Infrastructure.Auth;

public sealed class UserAuthService(AppDbContext db) : IUserAuthService
{
    private const int InitialCredits = 3;

    public async Task<User> FindOrCreateAsync(
        string provider,
        string providerUserId,
        string email,
        string? displayName,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.ExternalAuthAccounts
            .Include(a => a.User)
            .Where(a => a.Provider == provider && a.ProviderUserId == providerUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
            return existing.User;

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Users.Add(user);

        db.ExternalAuthAccounts.Add(new ExternalAuthAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = provider,
            ProviderUserId = providerUserId,
            CreatedAt = now,
        });

        db.UserCredits.Add(new UserCredits
        {
            UserId = user.Id,
            Balance = InitialCredits,
            UpdatedAt = now,
        });

        db.CreditLedgerEntries.Add(new CreditLedgerEntry
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Amount = InitialCredits,
            Reason = "initial_credits",
            CreatedAt = now,
        });

        await db.SaveChangesAsync(cancellationToken);

        return user;
    }
}

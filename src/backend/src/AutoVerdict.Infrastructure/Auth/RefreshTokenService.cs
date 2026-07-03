using System.Security.Cryptography;
using System.Text;
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutoVerdict.Infrastructure.Auth;

public sealed record RefreshResult(bool Succeeded, string? NewToken, Guid? UserId, string? UserEmail)
{
    public static readonly RefreshResult Failure = new(false, null, null, null);
}

public sealed class RefreshTokenService(
    AppDbContext db,
    IOptions<AuthOptions> options,
    TimeProvider clock)
{
    private readonly int _lifetimeDays = options.Value.RefreshTokenExpirationDays;

    public async Task<string> CreateFamilyAsync(Guid userId, CancellationToken ct = default)
    {
        var raw = GenerateRawToken();
        var now = clock.GetUtcNow();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(raw),
            FamilyId = Guid.NewGuid(),
            ExpiresAt = now.AddDays(_lifetimeDays),
            CreatedAt = now,
        });
        await db.SaveChangesAsync(ct);

        return raw;
    }

    public async Task<RefreshResult> RotateAsync(string rawToken, CancellationToken ct = default)
    {
        var token = await FindAsync(rawToken, ct);
        if (token is null)
            return RefreshResult.Failure;

        var now = clock.GetUtcNow();

        if (token.RevokedAt is not null)
        {
            // Reuse of a rotated/revoked token — assume theft, kill the family.
            await RevokeFamilyInternalAsync(token.FamilyId, now, ct);
            return RefreshResult.Failure;
        }

        if (token.ExpiresAt <= now)
            return RefreshResult.Failure;

        var newRaw = GenerateRawToken();
        var newHash = Hash(newRaw);

        token.RevokedAt = now;
        token.ReplacedByTokenHash = newHash;

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = token.UserId,
            TokenHash = newHash,
            FamilyId = token.FamilyId,
            ExpiresAt = now.AddDays(_lifetimeDays),
            CreatedAt = now,
        });
        await db.SaveChangesAsync(ct);

        return new RefreshResult(true, newRaw, token.UserId, token.User.Email);
    }

    public async Task RevokeFamilyAsync(string rawToken, CancellationToken ct = default)
    {
        var token = await FindAsync(rawToken, ct);
        if (token is null)
            return;

        await RevokeFamilyInternalAsync(token.FamilyId, clock.GetUtcNow(), ct);
    }

    private Task<RefreshToken?> FindAsync(string rawToken, CancellationToken ct)
    {
        var hash = Hash(rawToken);
        return db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
    }

    private async Task RevokeFamilyInternalAsync(Guid familyId, DateTimeOffset now, CancellationToken ct)
    {
        var active = await db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in active)
            t.RevokedAt = now;
        await db.SaveChangesAsync(ct);
    }

    private static string GenerateRawToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); // 256-bit

    private static string Hash(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}

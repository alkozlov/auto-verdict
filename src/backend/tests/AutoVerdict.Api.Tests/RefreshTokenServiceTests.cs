using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure.Auth;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace AutoVerdict.Api.Tests;

public sealed class RefreshTokenServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly FakeTimeProvider _clock;
    private readonly RefreshTokenService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public RefreshTokenServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();

        var now = new DateTimeOffset(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);
        _clock = new FakeTimeProvider(now);

        _db.Users.Add(new User
        {
            Id = _userId,
            Email = "user@example.com",
            CreatedAt = now,
            UpdatedAt = now,
        });
        _db.SaveChanges();

        _service = new RefreshTokenService(
            _db,
            Options.Create(new AuthOptions { RefreshTokenExpirationDays = 30 }),
            _clock);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateFamily_StoresHashedToken_NotRawToken()
    {
        var raw = await _service.CreateFamilyAsync(_userId);

        var row = await _db.RefreshTokens.SingleAsync();
        Assert.NotEqual(raw, row.TokenHash);
        Assert.Equal(64, row.TokenHash.Length); // SHA-256 hex
        Assert.Equal(_userId, row.UserId);
        Assert.Null(row.RevokedAt);
        Assert.Equal(_clock.GetUtcNow().AddDays(30), row.ExpiresAt);
    }

    [Fact]
    public async Task Rotate_ValidToken_IssuesNewTokenAndRevokesOld()
    {
        var raw = await _service.CreateFamilyAsync(_userId);
        var familyId = (await _db.RefreshTokens.SingleAsync()).FamilyId;

        var result = await _service.RotateAsync(raw);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.NewToken);
        Assert.NotEqual(raw, result.NewToken);
        Assert.Equal(_userId, result.UserId);
        Assert.Equal("user@example.com", result.UserEmail);

        var rows = (await _db.RefreshTokens.ToListAsync()).OrderBy(t => t.CreatedAt).ToList();
        Assert.Equal(2, rows.Count);
        Assert.NotNull(rows[0].RevokedAt);              // old revoked
        Assert.Equal(rows[1].TokenHash, rows[0].ReplacedByTokenHash);
        Assert.Null(rows[1].RevokedAt);                 // new active
        Assert.Equal(familyId, rows[1].FamilyId);       // same family
    }

    [Fact]
    public async Task Rotate_UnknownToken_Fails()
    {
        var result = await _service.RotateAsync("not-a-real-token");
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Rotate_ExpiredToken_Fails()
    {
        var raw = await _service.CreateFamilyAsync(_userId);
        _clock.Advance(TimeSpan.FromDays(31));

        var result = await _service.RotateAsync(raw);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Rotate_ReusedToken_RevokesWholeFamily()
    {
        var raw = await _service.CreateFamilyAsync(_userId);
        var first = await _service.RotateAsync(raw);
        Assert.True(first.Succeeded);

        // Move past the reuse grace window, then present the OLD token again — theft signal.
        _clock.Advance(TimeSpan.FromSeconds(61));
        var reuse = await _service.RotateAsync(raw);

        Assert.False(reuse.Succeeded);
        var rows = await _db.RefreshTokens.ToListAsync();
        Assert.All(rows, t => Assert.NotNull(t.RevokedAt)); // entire family dead

        // The rotated (previously valid) token must no longer work either.
        var afterTheft = await _service.RotateAsync(first.NewToken!);
        Assert.False(afterTheft.Succeeded);
    }

    [Fact]
    public async Task Rotate_ReusedWithinGraceWindow_RotatesNormally()
    {
        var raw = await _service.CreateFamilyAsync(_userId);
        var original = await _db.RefreshTokens.SingleAsync();
        var familyId = original.FamilyId;

        var first = await _service.RotateAsync(raw);
        Assert.True(first.Succeeded);
        var firstChildHash = original.ReplacedByTokenHash;
        Assert.NotNull(firstChildHash);

        // Second tab presents the same original token 30s later — concurrent refresh.
        _clock.Advance(TimeSpan.FromSeconds(30));
        var second = await _service.RotateAsync(raw);

        Assert.True(second.Succeeded);
        var rows = await _db.RefreshTokens.ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.All(rows, t => Assert.Equal(familyId, t.FamilyId)); // same family

        var children = rows.Where(t => t.Id != original.Id).ToList();
        Assert.Equal(2, children.Count);
        Assert.All(children, t => Assert.Null(t.RevokedAt)); // both children active

        // Original's chain pointer still references the FIRST child — not overwritten.
        Assert.Equal(firstChildHash, original.ReplacedByTokenHash);
    }

    [Fact]
    public async Task Rotate_WithinGraceAfterLogout_FailsAndFamilyStaysDead()
    {
        var rawA = await _service.CreateFamilyAsync(_userId);          // t=0: A
        var rotated = await _service.RotateAsync(rawA);                // t=0: A -> B
        Assert.True(rotated.Succeeded);

        await _service.RevokeFamilyAsync(rotated.NewToken!);           // logout kills the family

        // Still within A's 60s rotation grace — must NOT resurrect the dead family.
        _clock.Advance(TimeSpan.FromSeconds(30));
        var result = await _service.RotateAsync(rawA);

        Assert.False(result.Succeeded);
        var rows = await _db.RefreshTokens.ToListAsync();
        Assert.Equal(2, rows.Count);                        // no new live token minted
        Assert.All(rows, t => Assert.NotNull(t.RevokedAt)); // family stays dead
    }

    [Fact]
    public async Task Rotate_ReuseAtExactlyGraceBoundary_RotatesNormally()
    {
        var raw = await _service.CreateFamilyAsync(_userId);
        var first = await _service.RotateAsync(raw);
        Assert.True(first.Succeeded);

        _clock.Advance(TimeSpan.FromSeconds(60)); // exactly at the boundary — inclusive
        var second = await _service.RotateAsync(raw);

        Assert.True(second.Succeeded);
    }

    [Fact]
    public async Task Rotate_ReusedAfterGraceWindow_RevokesFamily()
    {
        var raw = await _service.CreateFamilyAsync(_userId);
        var first = await _service.RotateAsync(raw);
        Assert.True(first.Succeeded);

        _clock.Advance(TimeSpan.FromSeconds(61));
        var reuse = await _service.RotateAsync(raw);

        Assert.False(reuse.Succeeded);
        var rows = await _db.RefreshTokens.ToListAsync();
        Assert.All(rows, t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task RevokeFamily_KillsAllTokensInFamily()
    {
        var raw = await _service.CreateFamilyAsync(_userId);
        var rotated = await _service.RotateAsync(raw);

        await _service.RevokeFamilyAsync(rotated.NewToken!);

        var rows = await _db.RefreshTokens.ToListAsync();
        Assert.All(rows, t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task RevokeFamily_UnknownToken_DoesNotThrowAndChangesNothing()
    {
        var raw = await _service.CreateFamilyAsync(_userId);

        await _service.RevokeFamilyAsync("garbage");

        var row = await _db.RefreshTokens.SingleAsync();
        Assert.Null(row.RevokedAt); // existing session untouched
        _ = raw;
    }
}

using AutoVerdict.Contracts.Configuration;
using AutoVerdict.Application.Checks;
using AutoVerdict.Contracts.Enums;
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure.Checks;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutoVerdict.Api.Tests;

public sealed class CreditReservationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly Guid _userId = Guid.NewGuid();

    public CreditReservationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();

        var now = DateTimeOffset.UtcNow;
        _db.Users.Add(new User { Id = _userId, Email = "u@example.com", CreatedAt = now, UpdatedAt = now });
        _db.UserCredits.Add(new UserCredits { UserId = _userId, Balance = 1, UpdatedAt = now });
        _db.SaveChanges();

        // ExecuteUpdateAsync bypasses the change tracker, so any entity tracked
        // before it runs (like the seed row above) is never refreshed by later
        // tracking queries in this same context (EF Core identity-map behavior).
        // A real per-request DbContext never has this pre-existing tracked row,
        // so clear it here to match production shape rather than a test artifact.
        _db.ChangeTracker.Clear();
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    private CarCheckService CreateService(string whitelist = "") =>
        new(_db, Options.Create(new WhitelistOptions { Emails = whitelist }));

    private CarCheckResultService CreateResultService() => new(_db);

    private Task<CarCheck> CreateCheckAsync(CarCheckService svc, Guid checkId) =>
        svc.CreateAsync(_userId, checkId, "test check", null, "en", [], CancellationToken.None);

    [Fact]
    public async Task Create_ReservesCredit_AndWritesLedger()
    {
        var checkId = Guid.NewGuid();
        await CreateCheckAsync(CreateService(), checkId);

        Assert.Equal(0, (await _db.UserCredits.SingleAsync(c => c.UserId == _userId)).Balance);
        var entry = await _db.CreditLedgerEntries.SingleAsync(e => e.ReferenceId == checkId);
        Assert.Equal("car_check_reserved", entry.Reason);
        Assert.Equal(-1, entry.Amount);
    }

    [Fact]
    public async Task Create_WithZeroBalance_Throws_AndPersistsNothing()
    {
        await _db.UserCredits.Where(c => c.UserId == _userId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Balance, 0));

        var checkId = Guid.NewGuid();
        await Assert.ThrowsAsync<InsufficientCreditsException>(
            () => CreateCheckAsync(CreateService(), checkId));

        Assert.False(await _db.CarChecks.AnyAsync(c => c.CheckId == checkId));
        Assert.False(await _db.OutboxMessages.AnyAsync());
        Assert.False(await _db.CreditLedgerEntries.AnyAsync());
    }

    [Fact]
    public async Task Create_WhitelistedUser_DoesNotReserve()
    {
        await CreateCheckAsync(CreateService(whitelist: "u@example.com"), Guid.NewGuid());

        Assert.Equal(1, (await _db.UserCredits.SingleAsync(c => c.UserId == _userId)).Balance);
        Assert.False(await _db.CreditLedgerEntries.AnyAsync());
    }

    [Fact]
    public async Task Success_DoesNotTouchCredits()
    {
        var checkId = Guid.NewGuid();
        await CreateCheckAsync(CreateService(), checkId); // balance 1 -> 0

        await CreateResultService().RecordSuccessAsync(checkId, "key.md");

        Assert.Equal(0, (await _db.UserCredits.SingleAsync(c => c.UserId == _userId)).Balance);
        Assert.Single(_db.CreditLedgerEntries); // only the reservation
        Assert.Equal(CarCheckStatus.Completed, (await _db.CarChecks.SingleAsync()).Status);
    }

    [Fact]
    public async Task Failure_RefundsReservation_ExactlyOnce()
    {
        var checkId = Guid.NewGuid();
        await CreateCheckAsync(CreateService(), checkId); // balance 0
        var results = CreateResultService();

        await results.RecordFailureAsync(checkId, "boom");
        await results.RecordFailureAsync(checkId, "boom again"); // idempotent

        Assert.Equal(1, (await _db.UserCredits.SingleAsync(c => c.UserId == _userId)).Balance);
        Assert.Single(await _db.CreditLedgerEntries.Where(e => e.Reason == "car_check_refunded").ToListAsync());
    }

    [Fact]
    public async Task Failure_WithoutReservation_RefundsNothing()
    {
        // Simulates a whitelisted user's check or a pre-deploy check.
        var checkId = Guid.NewGuid();
        await CreateCheckAsync(CreateService(whitelist: "u@example.com"), checkId);

        await CreateResultService().RecordFailureAsync(checkId, "boom");

        Assert.Equal(1, (await _db.UserCredits.SingleAsync(c => c.UserId == _userId)).Balance);
        Assert.False(await _db.CreditLedgerEntries.AnyAsync());
    }
}

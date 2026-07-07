using AutoVerdict.Application.Payments;
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure.Payments;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AutoVerdict.Api.Tests;

public sealed class WebhookProcessorTests : IDisposable
{
    private sealed class FakePaymentService : IPaymentService
    {
        public bool ValidateWebhookSignature(string body, string signature) => signature != "bad";
        public Task<string> CreateCheckoutAsync(Guid userId, string email, string packageKey, string successUrl, string cancelUrl, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<string, PackagePrice>> GetPackagePricesAsync(CancellationToken ct = default) => throw new NotImplementedException();
    }

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly LemonSqueezyWebhookProcessor _processor;
    private readonly Guid _userId = Guid.NewGuid();

    public WebhookProcessorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        var now = DateTimeOffset.UtcNow;
        _db.Users.Add(new User { Id = _userId, Email = "u@example.com", CreatedAt = now, UpdatedAt = now });
        _db.UserCredits.Add(new UserCredits { UserId = _userId, Balance = 0, UpdatedAt = now });
        _db.SaveChanges();

        // ExecuteUpdateAsync (used by the processor to grant credits) bypasses the
        // change tracker, so the seed row tracked above would never be refreshed by
        // later tracking queries in this same context (EF Core identity-map
        // behavior; see CreditReservationTests for the same workaround). A real
        // per-request DbContext never has this pre-existing tracked row, so clear
        // it here to match production shape rather than a test artifact.
        _db.ChangeTracker.Clear();

        _processor = new LemonSqueezyWebhookProcessor(_db, new FakePaymentService());
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    private string OrderJson(string orderId, string status = "paid", string eventName = "order_created", string package = "credits_3") => $$"""
        {
          "meta": { "event_name": "{{eventName}}", "custom_data": { "user_id": "{{_userId}}", "package": "{{package}}" } },
          "data": { "id": "{{orderId}}", "attributes": { "status": "{{status}}" } }
        }
        """;

    [Fact]
    public async Task ValidOrder_GrantsCreditsOnce()
    {
        var outcome = await _processor.ProcessAsync(OrderJson("ord-1"), "sig", CancellationToken.None);

        Assert.Equal(WebhookOutcome.Processed, outcome);
        Assert.Equal(3, (await _db.UserCredits.SingleAsync()).Balance);
        Assert.Single(_db.PaymentOrders);
        Assert.Single(_db.CreditLedgerEntries);
    }

    [Fact]
    public async Task DuplicateOrder_SecondDelivery_GrantsNothing()
    {
        await _processor.ProcessAsync(OrderJson("ord-2"), "sig", CancellationToken.None);
        var second = await _processor.ProcessAsync(OrderJson("ord-2"), "sig", CancellationToken.None);

        Assert.Equal(WebhookOutcome.DuplicateOrder, second);
        Assert.Equal(3, (await _db.UserCredits.SingleAsync()).Balance);
        Assert.Single(_db.PaymentOrders);
    }

    [Fact]
    public async Task BadSignature_IsRejected()
    {
        var outcome = await _processor.ProcessAsync(OrderJson("ord-4"), "bad", CancellationToken.None);
        Assert.Equal(WebhookOutcome.InvalidSignature, outcome);
        Assert.Equal(0, (await _db.UserCredits.SingleAsync()).Balance);
    }

    [Theory]
    [InlineData("order_refunded", "paid")]
    [InlineData("order_created", "pending")]
    public async Task NonPaidOrOtherEvents_AreIgnored(string eventName, string status)
    {
        var outcome = await _processor.ProcessAsync(
            OrderJson("ord-5", status: status, eventName: eventName), "sig", CancellationToken.None);
        Assert.Equal(WebhookOutcome.Ignored, outcome);
        Assert.Equal(0, (await _db.UserCredits.SingleAsync()).Balance);
    }
}

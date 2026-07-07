# Pipeline & Billing Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop failed checks from burning AI budget on blind retries, make credits reservation-based, add Claude API backoff, and put the billing paths under test.

**Architecture:** Failure classification at the consumer (permanent → ACK+fail once; transient → delayed NAK with a `1m/4m/16m/30m` schedule, final attempt fails the check), a cross-attempt budget cap seeded from persisted `ai_runs` costs, an in-call retry policy around the Anthropic SDK, credit reserve-at-submission with ledger-driven idempotent refunds, and the webhook handler extracted into a testable processor.

**Tech Stack:** .NET 10, EF Core 10 (`ExecuteUpdateAsync` for provider-neutral atomic updates — the existing raw `NOW()` SQL is Postgres-only and breaks SQLite tests), NATS.Net (`NakAsync(delay:)`), xunit + SQLite in-memory + `FakeTimeProvider` (patterns already in `AutoVerdict.Api.Tests`), Anthropic SDK 12.23 exceptions.

**Spec:** `docs/superpowers/specs/2026-07-06-pipeline-billing-hardening-design.md`

## Global Constraints

- Ledger reasons (exact strings): reservation `car_check_reserved` (−1), refund `car_check_refunded` (+1). Ledger is append-only; both carry `ReferenceId = checkId`.
- NAK backoff schedule by delivery number: `1m, 4m, 16m, 30m` (delivery 5 = final, no NAK). `MaxDeliver` stays 5; a single shared const.
- AiRetryPolicy: max **3 attempts**, delays **2 s, 8 s** ±20 % jitter; retry ONLY rate-limit/5xx-service/transport errors and timeout-cancellation when the caller's token is NOT cancelled.
- Retryable SDK exceptions (verified in Anthropic.dll 12.23): `AnthropicRateLimitException`, `AnthropicServiceException`, `AnthropicIOException` (+ BCL `HttpRequestException`, conditional `TaskCanceledException`). Non-retryable: `AnthropicBadRequestException`, `AnthropicUnauthorizedException`, `AnthropicForbiddenException`, `AnthropicNotFoundException`, everything else. The implementer must confirm each type's namespace with a one-off grep of the package XML/dll and adjust `using`s only.
- Intermediate transient attempts must NOT set `Status = Failed` and must NOT publish `CarCheckFailed`.
- One schema migration (human-approved 2026-07-06 after Task 3 review): filtered unique index on `credit_ledger_entries (ReferenceId, Reason) WHERE ReferenceId IS NOT NULL` — the DB-level guard against double refunds. No other schema changes. No frontend changes. Whitelisted users (existing `WhitelistOptions`) never reserve/refund.
- Tests live in `src/backend/tests/AutoVerdict.Api.Tests`; run from `src/backend` with `dotnet test tests/AutoVerdict.Api.Tests`. Current suite: 12 tests — all must stay green.
- Work on branch `feature/pipeline-billing-hardening` (created from main before Task 1).
- Every commit message ends with trailer: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.

---

### Task 1: AiRetryPolicy + ClaudeAiClient wiring

**Files:**
- Create: `src/backend/src/AutoVerdict.Infrastructure/AI/AiRetryPolicy.cs`
- Modify: `src/backend/src/AutoVerdict.Infrastructure/AI/ClaudeAiClient.cs` (ctor + the `Messages.Create` call)
- Modify: `src/backend/src/AutoVerdict.Infrastructure/DependencyInjection.cs` (register singleton)
- Test: `src/backend/tests/AutoVerdict.Api.Tests/AiRetryPolicyTests.cs`

**Interfaces:**
- Produces: `sealed class AiRetryPolicy(TimeProvider clock, ILogger<AiRetryPolicy> logger)` with `Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)` and `internal static bool IsTransient(Exception ex, CancellationToken ct)`.

- [ ] **Step 1: Write the failing tests**

`AiRetryPolicyTests.cs`:

```csharp
using AutoVerdict.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace AutoVerdict.Api.Tests;

public sealed class AiRetryPolicyTests
{
    private static AiRetryPolicy Create(FakeTimeProvider clock) =>
        new(clock, NullLogger<AiRetryPolicy>.Instance);

    private static async Task<T> RunWithClock<T>(FakeTimeProvider clock, Task<T> task)
    {
        while (!task.IsCompleted)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            await Task.Yield();
        }
        return await task;
    }

    [Fact]
    public async Task Retries_TransientFailures_ThenSucceeds()
    {
        var clock = new FakeTimeProvider();
        var calls = 0;
        var task = Create(clock).ExecuteAsync(_ =>
        {
            calls++;
            if (calls < 3) throw new HttpRequestException("boom");
            return Task.FromResult(42);
        }, CancellationToken.None);

        Assert.Equal(42, await RunWithClock(clock, task));
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task GivesUp_AfterThreeAttempts()
    {
        var clock = new FakeTimeProvider();
        var calls = 0;
        var task = Create(clock).ExecuteAsync<int>(_ =>
        {
            calls++;
            throw new HttpRequestException("always");
        }, CancellationToken.None);

        await Assert.ThrowsAsync<HttpRequestException>(() => RunWithClock(clock, task));
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task DoesNotRetry_NonTransientErrors()
    {
        var clock = new FakeTimeProvider();
        var calls = 0;
        var task = Create(clock).ExecuteAsync<int>(_ =>
        {
            calls++;
            throw new InvalidOperationException("client bug");
        }, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => task);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task DoesNotRetry_WhenCallerCancelled()
    {
        var clock = new FakeTimeProvider();
        using var cts = new CancellationTokenSource();
        var calls = 0;
        var task = Create(clock).ExecuteAsync<int>(_ =>
        {
            calls++;
            cts.Cancel();
            throw new TaskCanceledException();
        }, cts.Token);

        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Timeout_WithLiveCallerToken_IsRetried()
    {
        var clock = new FakeTimeProvider();
        var calls = 0;
        var task = Create(clock).ExecuteAsync(_ =>
        {
            calls++;
            if (calls == 1) throw new TaskCanceledException(); // HttpClient timeout shape
            return Task.FromResult(1);
        }, CancellationToken.None);

        Assert.Equal(1, await RunWithClock(clock, task));
        Assert.Equal(2, calls);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run (from `src/backend`): `dotnet test tests/AutoVerdict.Api.Tests --filter AiRetryPolicyTests`
Expected: compilation FAILS — `AiRetryPolicy` does not exist.

- [ ] **Step 3: Implement**

`AiRetryPolicy.cs`:

```csharp
using Microsoft.Extensions.Logging;

namespace AutoVerdict.Infrastructure.AI;

/// <summary>
/// In-call retry for transient Anthropic API failures. Wraps a single stage
/// call; pipeline-level (cross-attempt) retries are handled by the NATS
/// consumer with its own backoff.
/// </summary>
public sealed class AiRetryPolicy(TimeProvider clock, ILogger<AiRetryPolicy> logger)
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan[] Delays = [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(8)];

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await operation(ct);
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsTransient(ex, ct))
            {
                var baseDelay = Delays[attempt - 1];
                var jitterFactor = 1 + ((Random.Shared.NextDouble() * 0.4) - 0.2); // ±20 %
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * jitterFactor);
                logger.LogWarning(ex,
                    "Transient AI API error (attempt {Attempt}/{Max}); retrying in {Delay}.",
                    attempt, MaxAttempts, delay);
                await Task.Delay(delay, clock, ct);
            }
        }
    }

    internal static bool IsTransient(Exception ex, CancellationToken ct) => ex switch
    {
        AnthropicRateLimitException => true,
        AnthropicServiceException => true,
        AnthropicIOException => true,
        HttpRequestException => true,
        TaskCanceledException when !ct.IsCancellationRequested => true,
        _ => false,
    };
}
```

Resolve the Anthropic exception namespaces first: `grep -aoE "Anthropic\.[A-Za-z.]*RateLimitException" src/backend/src/*/bin 2>/dev/null` or decompile-free check `grep -aoE "[A-Za-z.]+AnthropicRateLimitException" ~/.nuget/packages/anthropic/12.23.0/lib/net8.0/Anthropic.dll | sort -u` — add the correct `using`. If a listed type does not exist in the package, remove that arm and note it in your report (the `HttpRequestException`/`TaskCanceledException` arms are the safety net).

- [ ] **Step 4: Run tests**

`dotnet test tests/AutoVerdict.Api.Tests --filter AiRetryPolicyTests` → 5 PASS.

- [ ] **Step 5: Wire into ClaudeAiClient + DI**

In `ClaudeAiClient.cs`: change the class declaration and the API call:

```csharp
public sealed class ClaudeAiClient : IAiClient
{
    private const string ProviderName = "Claude";

    private readonly AnthropicClient _client;
    private readonly AiRetryPolicy _retryPolicy;

    public ClaudeAiClient(IOptions<ClaudeOptions> options, AiRetryPolicy retryPolicy)
    {
        _client = new AnthropicClient { ApiKey = options.Value.ApiKey };
        _retryPolicy = retryPolicy;
    }
```

and replace

```csharp
        Message response = await _client.Messages.Create(parameters, cancellationToken: cancellationToken);
```

with

```csharp
        Message response = await _retryPolicy.ExecuteAsync(
            ct => _client.Messages.Create(parameters, cancellationToken: ct),
            cancellationToken);
```

In `DependencyInjection.cs`, next to `services.AddSingleton<IAiClient, ClaudeAiClient>();` add (before it):

```csharp
        services.AddSingleton<AiRetryPolicy>();
```

(`TimeProvider.System` is already registered.)

- [ ] **Step 6: Full build + suite, commit**

`dotnet build AutoVerdict.sln` → 0 errors; `dotnet test tests/AutoVerdict.Api.Tests` → 17 PASS.

```bash
git add src/backend
git commit -m "feat: retry transient Anthropic API errors with backoff"
```

---

### Task 2: RetryDelays + PermanentCheckFailureException

**Files:**
- Create: `src/backend/src/AutoVerdict.ProcessingService/Pipeline/PermanentCheckFailureException.cs`
- Create: `src/backend/src/AutoVerdict.ProcessingService/Consumers/RetryDelays.cs`
- Test: `src/backend/tests/AutoVerdict.Api.Tests/RetryDelaysTests.cs`
- Modify: `src/backend/tests/AutoVerdict.Api.Tests/AutoVerdict.Api.Tests.csproj` (add ProcessingService project reference)

**Interfaces:**
- Produces: `PermanentCheckFailureException(string message, Exception? inner = null)` in `AutoVerdict.ProcessingService.Pipeline`; `static class RetryDelays` with `TimeSpan ForDelivery(ulong numDelivered)` in `AutoVerdict.ProcessingService.Consumers`.

- [ ] **Step 1: Reference + failing test**

```bash
dotnet add tests/AutoVerdict.Api.Tests reference src/AutoVerdict.ProcessingService/AutoVerdict.ProcessingService.csproj
```

`RetryDelaysTests.cs`:

```csharp
using AutoVerdict.ProcessingService.Consumers;

namespace AutoVerdict.Api.Tests;

public sealed class RetryDelaysTests
{
    [Theory]
    [InlineData(1ul, 1)]
    [InlineData(2ul, 4)]
    [InlineData(3ul, 16)]
    [InlineData(4ul, 30)]
    [InlineData(9ul, 30)]  // clamped
    [InlineData(0ul, 1)]   // defensive: metadata missing
    public void ForDelivery_FollowsSchedule(ulong delivered, int expectedMinutes) =>
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), RetryDelays.ForDelivery(delivered));
}
```

- [ ] **Step 2: Verify failure**

`dotnet test tests/AutoVerdict.Api.Tests --filter RetryDelaysTests` → compilation FAILS.

- [ ] **Step 3: Implement both types**

`RetryDelays.cs`:

```csharp
namespace AutoVerdict.ProcessingService.Consumers;

/// <summary>Backoff schedule for transient check-processing failures (per JetStream delivery count).</summary>
public static class RetryDelays
{
    private static readonly TimeSpan[] Schedule =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(4),
        TimeSpan.FromMinutes(16),
        TimeSpan.FromMinutes(30),
    ];

    public static TimeSpan ForDelivery(ulong numDelivered)
    {
        var index = Math.Clamp((int)numDelivered - 1, 0, Schedule.Length - 1);
        return Schedule[index];
    }
}
```

`PermanentCheckFailureException.cs`:

```csharp
namespace AutoVerdict.ProcessingService.Pipeline;

/// <summary>
/// A business-final pipeline failure: retrying cannot succeed (invalid report
/// after repair, AI budget exhausted). The consumer ACKs instead of NAKing.
/// </summary>
public sealed class PermanentCheckFailureException(string message, Exception? inner = null)
    : Exception(message, inner);
```

- [ ] **Step 4: Run + commit**

`dotnet test tests/AutoVerdict.Api.Tests` → 23 PASS (17 + 6). `dotnet build AutoVerdict.sln` → 0 errors.

```bash
git add src/backend
git commit -m "feat: retry schedule and permanent-failure exception for check processing"
```

---

### Task 3: Credit reservation + idempotent refund

**Files:**
- Modify: `src/backend/src/AutoVerdict.Infrastructure/Checks/CarCheckService.cs` (reserve instead of check)
- Modify: `src/backend/src/AutoVerdict.Infrastructure/Checks/CarCheckResultService.cs` (success loses credit logic; failure gains refund; whitelist dependency removed)
- Test: `src/backend/tests/AutoVerdict.Api.Tests/CreditReservationTests.cs`

**Interfaces:**
- Consumes: existing `WhitelistOptions`, `InsufficientCreditsException`, entities.
- Produces: unchanged public signatures (`CreateAsync`, `RecordSuccessAsync`, `RecordFailureAsync`); `CarCheckResultService` constructor becomes `(AppDbContext db)`.
- Ledger reasons: `car_check_reserved` / `car_check_refunded` (Global Constraints).

- [ ] **Step 1: Write the failing tests**

`CreditReservationTests.cs` (same SQLite fixture pattern as `RefreshTokenServiceTests` — copy the connection/db setup shape from there):

```csharp
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
```

(Adjust `WhitelistOptions` construction to its actual shape — it exposes an `Emails` string and a `Contains(string)`; check `AutoVerdict.Contracts/Configuration/WhitelistOptions.cs` and use whatever initializes it correctly.)

- [ ] **Step 2: Verify failure**

`dotnet test tests/AutoVerdict.Api.Tests --filter CreditReservationTests`
Expected: compile error (`CarCheckResultService` still takes whitelist) and/or failures on the not-yet-implemented behavior.

- [ ] **Step 3: Implement — CarCheckService.CreateAsync**

Replace the whitelist/credit block:

```csharp
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
```

(The method already opens a transaction before this point and commits after check+outbox `SaveChangesAsync` — the reservation joins that transaction, so an insufficient-credit throw rolls everything back. Delete the old `hasAvailableCredit` check and the duplicate `var now = DateTimeOffset.UtcNow;` further down.)

- [ ] **Step 4: Implement — CarCheckResultService**

Full new content:

```csharp
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

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }
}
```

- [ ] **Step 5: Run + full suite + commit**

`dotnet test tests/AutoVerdict.Api.Tests` → 29 PASS. `dotnet build AutoVerdict.sln` → 0 errors (confirms no other caller used the removed whitelist ctor param).

```bash
git add src/backend
git commit -m "feat: reserve credit at submission with ledger-driven idempotent refund"
```

---

### Task 4: LemonSqueezyWebhookProcessor extraction

**Files:**
- Create: `src/backend/src/AutoVerdict.Infrastructure/Payments/LemonSqueezyWebhookProcessor.cs`
- Modify: `src/backend/src/AutoVerdict.Api/Program.cs` (webhook endpoint → thin shell; the ~90-line handler body moves out verbatim-with-fixes)
- Modify: `src/backend/src/AutoVerdict.Infrastructure/DependencyInjection.cs` (`services.AddScoped<LemonSqueezyWebhookProcessor>();`)
- Test: `src/backend/tests/AutoVerdict.Api.Tests/WebhookProcessorTests.cs`

**Interfaces:**
- Produces: `enum WebhookOutcome { Processed, Ignored, InvalidSignature, DuplicateOrder }`; `sealed class LemonSqueezyWebhookProcessor(AppDbContext db, IPaymentService paymentService)` with `Task<WebhookOutcome> ProcessAsync(string body, string signature, CancellationToken ct)`.
- Consumes: existing `IPaymentService.ValidateWebhookSignature`, `CreditPackage.FindByKey`.

- [ ] **Step 1: Write the failing tests**

`WebhookProcessorTests.cs` (fake `IPaymentService` accepting any signature except `"bad"`; helper builds the LemonSqueezy JSON):

```csharp
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
```

(Check `IPaymentService`'s actual member list in `AutoVerdict.Application/Payments/IPaymentService.cs` and make the fake implement exactly that; if `PackagePrice` lives elsewhere, fix the using. The concurrent unique-violation → DuplicateOrder catch path cannot be deterministically interleaved in a unit test — it is covered by the sequential-duplicate test's outcome contract plus reviewer inspection of the catch block; say so in your report.)

- [ ] **Step 2: Verify failure**

`dotnet test tests/AutoVerdict.Api.Tests --filter WebhookProcessorTests` → compilation FAILS.

- [ ] **Step 3: Implement the processor**

`LemonSqueezyWebhookProcessor.cs` — move the endpoint body from `Program.cs` (the `app.MapPost("/api/billing/webhooks/lemonsqueezy", ...)` handler) with these changes:
- Signature check first: invalid → `WebhookOutcome.InvalidSignature`.
- All the "return Results.Ok()" ignore-paths → `return WebhookOutcome.Ignored;`.
- The `alreadyProcessed` early return → `return WebhookOutcome.DuplicateOrder;`.
- The raw `UPDATE user_credits ... NOW()` SQL → provider-neutral:
  ```csharp
  await db.UserCredits
      .Where(c => c.UserId == userId)
      .ExecuteUpdateAsync(s => s
          .SetProperty(c => c.Balance, c => c.Balance + package.Credits)
          .SetProperty(c => c.UpdatedAt, now), ct);
  ```
- Wrap the final `SaveChangesAsync` + `CommitAsync` in:
  ```csharp
  try
  {
      await db.SaveChangesAsync(ct);
      await tx.CommitAsync(ct);
  }
  catch (DbUpdateException ex) when (
      ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
  {
      // Concurrent duplicate delivery lost the race on the ExternalOrderId
      // unique index; the transaction rolled back the credit grant with it.
      return WebhookOutcome.DuplicateOrder;
  }
  return WebhookOutcome.Processed;
  ```
- Class shape:
  ```csharp
  public enum WebhookOutcome { Processed, Ignored, InvalidSignature, DuplicateOrder }

  public sealed class LemonSqueezyWebhookProcessor(AppDbContext db, IPaymentService paymentService)
  {
      public async Task<WebhookOutcome> ProcessAsync(string body, string signature, CancellationToken ct)
      { /* moved logic */ }
  }
  ```

- [ ] **Step 4: Thin endpoint + DI**

Replace the whole webhook endpoint in `Program.cs` with:

```csharp
app.MapPost("/api/billing/webhooks/lemonsqueezy", async (
    HttpContext ctx,
    LemonSqueezyWebhookProcessor processor,
    CancellationToken ct) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync(ct);

    var signature = ctx.Request.Headers["X-Signature"].FirstOrDefault()
                 ?? ctx.Request.Headers["X-Lemon-Squeezy-Signature"].FirstOrDefault()
                 ?? string.Empty;

    var outcome = await processor.ProcessAsync(body, signature, ct);
    return outcome == WebhookOutcome.InvalidSignature ? Results.Unauthorized() : Results.Ok();
}).DisableAntiforgery();
```

DI (`DependencyInjection.cs`, near the payment service registration):

```csharp
        services.AddScoped<LemonSqueezyWebhookProcessor>();
```

- [ ] **Step 5: Run + commit**

`dotnet test tests/AutoVerdict.Api.Tests` → 34 PASS. `dotnet build AutoVerdict.sln` → 0 errors.

```bash
git add src/backend
git commit -m "feat: extract testable LemonSqueezy webhook processor with graceful duplicate handling"
```

---

### Task 5: Consumer classification + Processing status + budget seeding

**Files:**
- Modify: `src/backend/src/AutoVerdict.ProcessingService/Consumers/CarCheckConsumer.cs`
- Modify: `src/backend/src/AutoVerdict.ProcessingService/Pipeline/CarCheckAnalysisPipeline.cs`
- Create: `src/backend/src/AutoVerdict.ProcessingService/Pipeline/AiSpend.cs`
- Test: `src/backend/tests/AutoVerdict.Api.Tests/AiSpendTests.cs`

**Interfaces:**
- Consumes: `PermanentCheckFailureException`, `RetryDelays.ForDelivery(ulong)` (Task 2).
- Produces: `static class AiSpend` with `Task<decimal> SumPriorForCheckAsync(AppDbContext db, Guid checkId, CancellationToken ct)`.

- [ ] **Step 1: Failing test for AiSpend**

`AiSpendTests.cs`:

```csharp
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure.Persistence;
using AutoVerdict.ProcessingService.Pipeline;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AutoVerdict.Api.Tests;

public sealed class AiSpendTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public AiSpendTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    private void AddRun(Guid checkId, decimal cost)
    {
        var now = DateTimeOffset.UtcNow;
        _db.AiRuns.Add(new AiRun
        {
            CheckId = checkId, Stage = "FactExtraction", Provider = "Claude",
            Model = "m", PromptVersion = "1", EstimatedCostEur = cost,
            Status = "Succeeded", StartedAt = now, CreatedAt = now,
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task SumsOnlyThisChecksRuns()
    {
        var checkId = Guid.NewGuid();
        AddRun(checkId, 0.30m);
        AddRun(checkId, 0.25m);
        AddRun(Guid.NewGuid(), 5.00m); // different check — excluded

        var sum = await AiSpend.SumPriorForCheckAsync(_db, checkId, CancellationToken.None);

        Assert.Equal(0.55m, sum, precision: 4);
    }

    [Fact]
    public async Task NoRuns_SumsToZero()
    {
        Assert.Equal(0m, await AiSpend.SumPriorForCheckAsync(_db, Guid.NewGuid(), CancellationToken.None));
    }
}
```

- [ ] **Step 2: Verify failure, implement AiSpend**

`dotnet test tests/AutoVerdict.Api.Tests --filter AiSpendTests` → compilation FAILS. Then `AiSpend.cs`:

```csharp
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoVerdict.ProcessingService.Pipeline;

public static class AiSpend
{
    /// <summary>
    /// Total estimated cost of all persisted AI runs for a check (across every
    /// prior attempt). Summed as double because SQLite (used in tests) cannot
    /// aggregate decimals server-side; sub-cent precision loss is irrelevant.
    /// </summary>
    public static async Task<decimal> SumPriorForCheckAsync(
        AppDbContext db, Guid checkId, CancellationToken ct)
    {
        var sum = await db.AiRuns
            .Where(r => r.CheckId == checkId)
            .Select(r => (double)r.EstimatedCostEur)
            .SumAsync(ct);
        return (decimal)sum;
    }
}
```

Run: `dotnet test tests/AutoVerdict.Api.Tests --filter AiSpendTests` → 2 PASS.

- [ ] **Step 3: Pipeline — seed budget, permanent classifications**

In `CarCheckAnalysisPipeline.ExecuteAsync`, replace

```csharp
        var budget = new AiBudgetTracker(_aiPipelineOptions.HardBudgetEur);
```

with

```csharp
        var budget = new AiBudgetTracker(_aiPipelineOptions.HardBudgetEur);
        var priorSpend = await GetPriorSpendAsync(message.CheckId, cancellationToken);
        if (priorSpend > 0)
        {
            budget.Add(priorSpend);
            logger.LogInformation(
                "Check {CheckId} carries {PriorSpend} EUR of AI spend from previous attempts.",
                message.CheckId, priorSpend);
        }
        if (budget.SpentEur >= budget.HardBudgetEur)
            throw new PermanentCheckFailureException(
                $"AI budget exhausted for this check across attempts ({budget.SpentEur} of {budget.HardBudgetEur} EUR).");
```

Add the private helper (next to `IsFreeReviewAsync`, same scope pattern):

```csharp
    private async Task<decimal> GetPriorSpendAsync(Guid checkId, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await AiSpend.SumPriorForCheckAsync(db, checkId, ct);
    }
```

And replace the validation-failure throw

```csharp
                throw new InvalidOperationException(
                    "AI report failed validation after repair: " + string.Join("; ", validation.Errors));
```

with

```csharp
                throw new PermanentCheckFailureException(
                    "AI report failed validation after repair: " + string.Join("; ", validation.Errors));
```

- [ ] **Step 4: Consumer — classification, Processing status, delayed NAK**

Rework `CarCheckConsumer.cs`:

1. Add `private const int MaxDeliver = 5;` and use it in `CreateConsumerAsync`'s `MaxDeliver = MaxDeliver`.
2. The consume loop passes delivery metadata and naks with delay:

```csharp
        await foreach (var msg in consumer.ConsumeAsync<CarCheckRequestedMessage?>(
            serializer: NatsJsonSerializer<CarCheckRequestedMessage?>.Default,
            cancellationToken: stoppingToken))
        {
            if (msg.Data is not { } data)
            {
                logger.LogWarning("Received empty message on {Subject}, acking.", msg.Subject);
                await msg.AckAsync(cancellationToken: stoppingToken);
                continue;
            }

            var numDelivered = msg.Metadata?.NumDelivered ?? 1;
            var (shouldAck, retryDelay) = await ProcessMessageAsync(data, js, numDelivered, stoppingToken);
            if (shouldAck) await msg.AckAsync(cancellationToken: stoppingToken);
            else await msg.NakAsync(delay: retryDelay, cancellationToken: stoppingToken);
        }
```

3. `ProcessMessageAsync` becomes:

```csharp
    private async Task<(bool shouldAck, TimeSpan retryDelay)> ProcessMessageAsync(
        CarCheckRequestedMessage data,
        NatsJSContext js,
        ulong numDelivered,
        CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Processing check {CheckId} (delivery {Delivery}).", data.CheckId, numDelivered);

            if (await IsTerminalAsync(data.CheckId, ct))
            {
                logger.LogInformation(
                    "Check {CheckId} already terminal; acknowledging duplicate message.", data.CheckId);
                return (true, default);
            }

            await MarkProcessingAsync(data.CheckId, ct);

            var storageKey = await pipeline.ExecuteAsync(data, ct);
            await RecordAndPublishSuccessAsync(js, data, storageKey, ct);

            logger.LogInformation("Check {CheckId} completed.", data.CheckId);
            return (true, default);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (PermanentCheckFailureException ex)
        {
            logger.LogError(ex, "Permanent failure for check {CheckId}; not retrying.", data.CheckId);
            await TryRecordAndPublishFailureAsync(js, data, ex.Message, ct);
            return (true, default);
        }
        catch (Exception ex)
        {
            if (numDelivered >= MaxDeliver)
            {
                logger.LogError(ex,
                    "Check {CheckId} failed on final attempt {Attempt}; marking failed.",
                    data.CheckId, numDelivered);
                await TryRecordAndPublishFailureAsync(js, data, ex.Message, ct);
                return (true, default);
            }

            var delay = RetryDelays.ForDelivery(numDelivered);
            logger.LogWarning(ex,
                "Transient failure for check {CheckId} (attempt {Attempt}/{Max}); retrying in {Delay}.",
                data.CheckId, numDelivered, MaxDeliver, delay);
            return (false, delay);
        }
    }
```

4. Replace `IsAlreadyCompletedAsync` with terminal check + add `MarkProcessingAsync`:

```csharp
    private async Task<bool> IsTerminalAsync(Guid checkId, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.CarChecks
            .AsNoTracking()
            .AnyAsync(c => c.CheckId == checkId &&
                (c.Status == CarCheckStatus.Completed || c.Status == CarCheckStatus.Failed), ct);
    }

    private async Task MarkProcessingAsync(Guid checkId, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.CarChecks
            .Where(c => c.CheckId == checkId && c.Status == CarCheckStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, CarCheckStatus.Processing)
                .SetProperty(c => c.UpdatedAt, DateTimeOffset.UtcNow), ct);
    }
```

Add `using AutoVerdict.ProcessingService.Pipeline;` for the exception type (already imported) and keep `RecordAndPublishSuccessAsync`/`TryRecordAndPublishFailureAsync` unchanged.

NOTE (behavior): a check that already failed terminally stays Failed even if a duplicate message arrives — `IsTerminalAsync` covers Completed AND Failed now, which is required because permanent failures ACK (a crash between record and ack must not reprocess a Failed check).

- [ ] **Step 5: Full suite + build + commit**

`dotnet test tests/AutoVerdict.Api.Tests` → 36 PASS. `dotnet build AutoVerdict.sln` → 0 errors.

```bash
git add src/backend
git commit -m "feat: classify check failures, delayed retries, cross-attempt budget cap, Processing status"
```

---

### Task 6: Whole-suite verification, review, merge, deploy (controller-run)

- [ ] **Step 1:** `dotnet test` (whole solution) — all green; `npm run build` untouched (no frontend changes; skip).
- [ ] **Step 2:** Final whole-branch review per superpowers:requesting-code-review, most capable model, package via `scripts/review-package $(git merge-base main HEAD) HEAD`.
- [ ] **Step 3:** Merge `feature/pipeline-billing-hardening` → main (no-ff), push.
- [ ] **Step 4:** Deploy PRD workflow; verify live: `/api/me` 401, frontend 200, API + processing logs clean startup ("NATS JetStream consumer ready"), no errors.
- [ ] **Step 5:** Optional live E2E with the human: submit one real check (~0.1–0.7 EUR AI spend) and watch it go Pending → Processing → Completed with the credit reserved at submission (balance drops immediately) — the first true end-to-end of the new semantics.
- [ ] **Step 6:** Update spec status to Implemented; ledger/memory bookkeeping.

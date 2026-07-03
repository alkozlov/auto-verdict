# Auth Session Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the single 30-day JWT with a 30-minute access token + DB-backed rotating refresh-token cookie, and make the SPA refresh/logout cleanly on 401.

**Architecture:** New `refresh_tokens` table + `RefreshTokenService` (create family / rotate / revoke). Three endpoint changes in `AutoVerdict.Api/Program.cs` (OAuth complete sets cookie, new `POST /api/auth/refresh`, new `POST /api/auth/logout`). Frontend keeps the access token in memory only, with a single-flight refresh and a shared `authFetch` wrapper that retries once on 401.

**Tech Stack:** .NET 10 minimal APIs, EF Core 10 + Npgsql, xunit + EF Core Sqlite (in-memory) for tests, React SPA (Vite).

**Spec:** `docs/superpowers/specs/2026-07-03-auth-refresh-design.md`

## Global Constraints

- Access token lifetime: **30 minutes** (`Auth:JwtExpirationMinutes`, default 30). `JwtExpirationDays` is removed.
- Refresh token lifetime: **30 days sliding** (`Auth:RefreshTokenExpirationDays`, default 30).
- Cookie: name `av_refresh`, HttpOnly, Secure, SameSite=Lax, Path=`/api/auth`.
- Refresh tokens stored **SHA-256-hashed only** — the raw value never touches the DB or logs.
- Reuse of a rotated/revoked refresh token revokes its whole `FamilyId`.
- Auth endpoints never 500 on bad input: invalid/missing token ⇒ 401 (refresh) / 204 (logout).
- Follow existing codebase patterns: `sealed` classes, primary constructors, snake_case table names, one `IEntityTypeConfiguration` per entity in `Persistence/Configurations`.
- Working directory for all backend commands: `src/backend`.
- Frontend build check: `npm run build` in `src/frontend`.

---

### Task 1: `RefreshToken` entity, EF configuration, migration

**Files:**
- Create: `src/backend/src/AutoVerdict.Domain/Entities/RefreshToken.cs`
- Create: `src/backend/src/AutoVerdict.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`
- Modify: `src/backend/src/AutoVerdict.Infrastructure/Persistence/AppDbContext.cs` (add DbSet)
- Generated: `src/backend/src/AutoVerdict.Infrastructure/Persistence/Migrations/*_AddRefreshTokens.cs`

**Interfaces:**
- Produces: entity `AutoVerdict.Domain.Entities.RefreshToken` with properties `Guid Id`, `Guid UserId`, `string TokenHash`, `Guid FamilyId`, `DateTimeOffset ExpiresAt`, `DateTimeOffset CreatedAt`, `DateTimeOffset? RevokedAt`, `string? ReplacedByTokenHash`, `User User`; DbSet `AppDbContext.RefreshTokens`.

- [ ] **Step 1: Create the entity**

`src/backend/src/AutoVerdict.Domain/Entities/RefreshToken.cs`:

```csharp
namespace AutoVerdict.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public Guid FamilyId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public User User { get; set; } = null!;
}
```

- [ ] **Step 2: Create the EF configuration**

`src/backend/src/AutoVerdict.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`:

```csharp
using AutoVerdict.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVerdict.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64); // SHA-256 hex

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.FamilyId);

        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.Property(t => t.ReplacedByTokenHash)
            .HasMaxLength(64);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 3: Register the DbSet**

In `src/backend/src/AutoVerdict.Infrastructure/Persistence/AppDbContext.cs`, after the `AiRuns` DbSet line, add:

```csharp
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
```

- [ ] **Step 4: Build**

Run (from `src/backend`): `dotnet build AutoVerdict.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Generate the migration**

Run (from `src/backend`):
```bash
dotnet ef migrations add AddRefreshTokens \
  --project src/AutoVerdict.Infrastructure \
  --startup-project src/AutoVerdict.Infrastructure
```
(`DesignTimeAppDbContextFactory` supplies the connection string; no DB needs to be running.)
Expected: new files `*_AddRefreshTokens.cs` + updated `AppDbContextModelSnapshot.cs` under `Persistence/Migrations`. Open the migration and confirm it creates table `refresh_tokens` with unique index on `TokenHash` and indexes on `UserId`, `FamilyId` — and nothing else (no changes to other tables).

- [ ] **Step 6: Commit**

```bash
git add src/backend
git commit -m "feat: add refresh_tokens table and entity"
```

---

### Task 2: Test project scaffold + `RefreshTokenService` (TDD)

**Files:**
- Create: `src/backend/tests/AutoVerdict.Api.Tests/AutoVerdict.Api.Tests.csproj`
- Create: `src/backend/tests/AutoVerdict.Api.Tests/RefreshTokenServiceTests.cs`
- Create: `src/backend/src/AutoVerdict.Infrastructure/Auth/RefreshTokenService.cs`
- Modify: `src/backend/AutoVerdict.sln` (add test project)
- Modify: `src/backend/src/AutoVerdict.Infrastructure/DependencyInjection.cs` (register service + TimeProvider)

**Interfaces:**
- Consumes: `RefreshToken` entity and `AppDbContext.RefreshTokens` from Task 1; existing `AuthOptions` (Task 3 adds `RefreshTokenExpirationDays`, but this task defines it — see Step 3).
- Produces:
  - `sealed record RefreshResult(bool Succeeded, string? NewToken, Guid? UserId, string? UserEmail)` in namespace `AutoVerdict.Infrastructure.Auth`
  - `sealed class RefreshTokenService(AppDbContext db, IOptions<AuthOptions> options, TimeProvider clock)` with:
    - `Task<string> CreateFamilyAsync(Guid userId, CancellationToken ct = default)` — returns the raw token
    - `Task<RefreshResult> RotateAsync(string rawToken, CancellationToken ct = default)`
    - `Task RevokeFamilyAsync(string rawToken, CancellationToken ct = default)`
  - `AuthOptions.RefreshTokenExpirationDays` (int, default 30)

- [ ] **Step 1: Create the test project and wire it into the solution**

Run (from `src/backend`):
```bash
dotnet new xunit -o tests/AutoVerdict.Api.Tests --name AutoVerdict.Api.Tests
dotnet add tests/AutoVerdict.Api.Tests package Microsoft.EntityFrameworkCore.Sqlite
dotnet add tests/AutoVerdict.Api.Tests reference src/AutoVerdict.Infrastructure/AutoVerdict.Infrastructure.csproj
dotnet sln AutoVerdict.sln add tests/AutoVerdict.Api.Tests/AutoVerdict.Api.Tests.csproj
```
Delete the template's `UnitTest1.cs`. Verify the csproj targets `net10.0`; if the template produced a different TFM, set `<TargetFramework>net10.0</TargetFramework>`.

- [ ] **Step 2: Add `RefreshTokenExpirationDays` to AuthOptions**

In `src/backend/src/AutoVerdict.Infrastructure/Auth/AuthOptions.cs`, after `JwtExpirationDays`, add (leave `JwtExpirationDays` alone for now — Task 3 replaces it):

```csharp
    public int RefreshTokenExpirationDays { get; set; } = 30;
```

- [ ] **Step 3: Write the failing tests**

`src/backend/tests/AutoVerdict.Api.Tests/RefreshTokenServiceTests.cs`:

```csharp
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

        var rows = await _db.RefreshTokens.OrderBy(t => t.CreatedAt).ToListAsync();
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

        // Present the OLD token again — theft signal.
        var reuse = await _service.RotateAsync(raw);

        Assert.False(reuse.Succeeded);
        var rows = await _db.RefreshTokens.ToListAsync();
        Assert.All(rows, t => Assert.NotNull(t.RevokedAt)); // entire family dead

        // The rotated (previously valid) token must no longer work either.
        var afterTheft = await _service.RotateAsync(first.NewToken!);
        Assert.False(afterTheft.Succeeded);
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
```

Also add the fake-clock package (from `src/backend`):
```bash
dotnet add tests/AutoVerdict.Api.Tests package Microsoft.Extensions.TimeProvider.Testing
```

- [ ] **Step 4: Run tests to verify they fail**

Run: `dotnet test tests/AutoVerdict.Api.Tests`
Expected: compilation FAILS — `RefreshTokenService` / `RefreshResult` do not exist yet. That's the failing state for TDD on new types.

- [ ] **Step 5: Implement `RefreshTokenService`**

`src/backend/src/AutoVerdict.Infrastructure/Auth/RefreshTokenService.cs`:

```csharp
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
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/AutoVerdict.Api.Tests`
Expected: PASS, 7 tests.

- [ ] **Step 7: Register in DI**

In `src/backend/src/AutoVerdict.Infrastructure/DependencyInjection.cs`, next to `services.AddSingleton<JwtService>();`, add:

```csharp
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<RefreshTokenService>();
```

Run: `dotnet build AutoVerdict.sln` — expected: 0 errors.

- [ ] **Step 8: Commit**

```bash
git add src/backend
git commit -m "feat: add RefreshTokenService with rotation and family revocation"
```

---

### Task 3: 30-minute access tokens (TDD)

**Files:**
- Create: `src/backend/tests/AutoVerdict.Api.Tests/JwtServiceTests.cs`
- Modify: `src/backend/src/AutoVerdict.Infrastructure/Auth/AuthOptions.cs`
- Modify: `src/backend/src/AutoVerdict.Infrastructure/Auth/JwtService.cs`

**Interfaces:**
- Produces: `AuthOptions.JwtExpirationMinutes` (int, default 30). `JwtExpirationDays` is deleted. `JwtService.GenerateToken(Guid, string)` signature unchanged.

- [ ] **Step 1: Write the failing test**

`src/backend/tests/AutoVerdict.Api.Tests/JwtServiceTests.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using AutoVerdict.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace AutoVerdict.Api.Tests;

public sealed class JwtServiceTests
{
    [Fact]
    public void GenerateToken_ExpiresInThirtyMinutes()
    {
        var service = new JwtService(Options.Create(new AuthOptions
        {
            JwtSecret = "test-secret-that-is-long-enough-for-hs256!!",
            JwtExpirationMinutes = 30,
        }));

        var before = DateTime.UtcNow;
        var token = service.GenerateToken(Guid.NewGuid(), "user@example.com");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var expected = before.AddMinutes(30);
        Assert.InRange(jwt.ValidTo, expected.AddSeconds(-30), expected.AddSeconds(90));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/AutoVerdict.Api.Tests --filter JwtServiceTests`
Expected: compilation FAILS — `JwtExpirationMinutes` does not exist.

- [ ] **Step 3: Implement**

In `src/backend/src/AutoVerdict.Infrastructure/Auth/AuthOptions.cs`, replace

```csharp
    public int JwtExpirationDays { get; set; } = 30;
```

with

```csharp
    public int JwtExpirationMinutes { get; set; } = 30;
```

In `src/backend/src/AutoVerdict.Infrastructure/Auth/JwtService.cs`, replace

```csharp
            expires: DateTime.UtcNow.AddDays(_options.JwtExpirationDays),
```

with

```csharp
            expires: DateTime.UtcNow.AddMinutes(_options.JwtExpirationMinutes),
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test tests/AutoVerdict.Api.Tests`
Expected: PASS, 8 tests. Also run `dotnet build AutoVerdict.sln` — 0 errors (confirms nothing else referenced `JwtExpirationDays`).

- [ ] **Step 5: Commit**

```bash
git add src/backend
git commit -m "feat: shorten access tokens to 30 minutes"
```

---

### Task 4: Auth endpoints — cookie on OAuth complete, refresh, logout

**Files:**
- Modify: `src/backend/src/AutoVerdict.Api/Program.cs` (OAuth complete handler ~lines 128-151; new endpoints after it; helper functions at the bottom near `GetUserId`)

**Interfaces:**
- Consumes: `RefreshTokenService.CreateFamilyAsync/RotateAsync/RevokeFamilyAsync`, `RefreshResult` (Task 2); `JwtService.GenerateToken` (Task 3).
- Produces: `POST /api/auth/refresh` → `200 {"accessToken": "..."}` or `401`; `POST /api/auth/logout` → `204`; `GET /api/auth/google/complete` → redirect to `/auth/callback` (no query string) with `av_refresh` cookie set.

- [ ] **Step 1: Add cookie helpers**

The `const` MUST go near the top of `Program.cs` — right after `var app = builder.Build();` — because in top-level statements a const cannot be referenced above its declaration (unlike `static` local functions):

```csharp
const string RefreshCookieName = "av_refresh";
```

Then at the bottom of `Program.cs`, next to the other `static` helpers (`GetUserId`, `GetBaseUrl`), add:

```csharp
static void SetRefreshCookie(HttpContext ctx, string token, int days) =>
    ctx.Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/api/auth",
        MaxAge = TimeSpan.FromDays(days),
    });

static void ClearRefreshCookie(HttpContext ctx) =>
    ctx.Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/api/auth" });
```

- [ ] **Step 2: Rework the OAuth completion handler**

In the `GET /api/auth/google/complete` handler, replace the injected `JwtService jwtService` parameter with `RefreshTokenService refreshTokenService, IOptions<AuthOptions> authOpts`, and replace

```csharp
    var token = jwtService.GenerateToken(user.Id, user.Email);
    return Results.Redirect($"/auth/callback?token={Uri.EscapeDataString(token)}");
```

with

```csharp
    var refreshToken = await refreshTokenService.CreateFamilyAsync(user.Id, ct);
    SetRefreshCookie(ctx, refreshToken, authOpts.Value.RefreshTokenExpirationDays);
    return Results.Redirect("/auth/callback");
```

- [ ] **Step 3: Add refresh and logout endpoints**

Directly after the `google/complete` endpoint:

```csharp
app.MapPost("/api/auth/refresh", async (
    HttpContext ctx,
    RefreshTokenService refreshTokenService,
    JwtService jwtService,
    IOptions<AuthOptions> authOpts,
    CancellationToken ct) =>
{
    var raw = ctx.Request.Cookies[RefreshCookieName];
    if (string.IsNullOrEmpty(raw))
        return Results.Unauthorized();

    var result = await refreshTokenService.RotateAsync(raw, ct);
    if (!result.Succeeded)
    {
        ClearRefreshCookie(ctx);
        return Results.Unauthorized();
    }

    SetRefreshCookie(ctx, result.NewToken!, authOpts.Value.RefreshTokenExpirationDays);
    var accessToken = jwtService.GenerateToken(result.UserId!.Value, result.UserEmail!);
    return Results.Ok(new { accessToken });
});

app.MapPost("/api/auth/logout", async (
    HttpContext ctx,
    RefreshTokenService refreshTokenService,
    CancellationToken ct) =>
{
    var raw = ctx.Request.Cookies[RefreshCookieName];
    if (!string.IsNullOrEmpty(raw))
        await refreshTokenService.RevokeFamilyAsync(raw, ct);

    ClearRefreshCookie(ctx);
    return Results.NoContent();
});
```

- [ ] **Step 4: Build and run backend tests**

Run: `dotnet build AutoVerdict.sln && dotnet test tests/AutoVerdict.Api.Tests`
Expected: build 0 errors; 8 tests PASS.

- [ ] **Step 5: Smoke-test locally (compose)**

Run from repo root: `podman compose up -d --build auto-verdict-api postgres nats nats-setup seaweedfs seaweedfs-setup`
Then:
```bash
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5000/api/auth/refresh
```
Expected: `401` (no cookie — proves the endpoint exists and fails closed).
```bash
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5000/api/auth/logout
```
Expected: `204`.

- [ ] **Step 6: Commit**

```bash
git add src/backend
git commit -m "feat: refresh-cookie auth endpoints (refresh, logout, cookie on OAuth complete)"
```

---

### Task 5: Frontend — in-memory token + single-flight refresh + authFetch

> **Execution note:** Tasks 5 and 6 are executed together as ONE task (per human decision 2026-07-03): the lib rewrite breaks the four caller files until Task 6 updates them, and no commit may have a red `npm run build`. Do all Task 5 + Task 6 steps, then build, then commit once.

**Files:**
- Rewrite: `src/frontend/src/lib/auth.ts`
- Modify: `src/frontend/src/lib/api.ts`

**Interfaces:**
- Produces (from `lib/auth.ts`): `getAccessToken(): string | null`, `setAccessToken(token: string | null): void`, `refreshAccessToken(): Promise<boolean>` (single-flight), `logout(): Promise<void>`.
- Produces (from `lib/api.ts`): existing `api.*` surface unchanged for callers; all requests now flow through `authFetch` with one 401-refresh-retry.
- Removed: `TOKEN_KEY`, `getToken`, `setToken`, `removeToken` (Task 6 fixes the two components importing `removeToken`).

- [ ] **Step 1: Rewrite `lib/auth.ts`**

```typescript
// Access token lives in memory only; the session is carried by the HttpOnly
// av_refresh cookie (Path=/api/auth), which JS can never read.
let accessToken: string | null = null;
let refreshPromise: Promise<boolean> | null = null;

// One-time cleanup of the legacy localStorage token.
if (typeof window !== "undefined") {
  localStorage.removeItem("av_token");
}

export function getAccessToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

/**
 * Exchange the refresh cookie for a new access token.
 * Single-flight: concurrent callers share one in-flight request.
 * Resolves true on success, false when the session is gone.
 */
export function refreshAccessToken(): Promise<boolean> {
  if (!refreshPromise) {
    refreshPromise = (async () => {
      try {
        const res = await fetch("/api/auth/refresh", { method: "POST" });
        if (!res.ok) {
          accessToken = null;
          return false;
        }
        const data = (await res.json()) as { accessToken: string };
        accessToken = data.accessToken;
        return true;
      } catch {
        accessToken = null;
        return false;
      } finally {
        refreshPromise = null;
      }
    })();
  }
  return refreshPromise;
}

export async function logout(): Promise<void> {
  try {
    await fetch("/api/auth/logout", { method: "POST" });
  } catch {
    // Best effort — clear local state regardless.
  }
  accessToken = null;
}
```

- [ ] **Step 2: Rework `lib/api.ts` around `authFetch`**

Replace the import and the `request` helper at the top of `api.ts`:

```typescript
import { getAccessToken, refreshAccessToken, setAccessToken } from "./auth";
import { i18n } from "@/i18n";

async function authFetch(
  input: string,
  init: RequestInit = {},
  allowRetry = true,
): Promise<Response> {
  const headers: Record<string, string> = {
    ...(init.headers as Record<string, string>),
  };
  const token = getAccessToken();
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const res = await fetch(input, { ...init, headers });

  if (res.status === 401 && allowRetry) {
    if (await refreshAccessToken()) {
      return authFetch(input, init, false);
    }
    setAccessToken(null);
    window.location.assign("/?session=expired");
    throw new Error("401: session expired");
  }
  return res;
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const res = await authFetch(`/api${path}`, options);
  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(`${res.status}: ${text || res.statusText}`);
  }
  return res.json();
}
```

Then update the three hand-rolled fetches to use `authFetch` (delete their local token/header code):

- `checks.downloadPdf`: replace the `const token ... fetch(...)` block with
  ```typescript
  const res = await authFetch(`/api/checks/${id}/pdf`);
  ```
- `checks.create`: replace the token/header/fetch block with
  ```typescript
  const res = await authFetch("/api/checks", { method: "POST", body: form });
  ```
- `uploads.upload`: replace the token/header/fetch block with
  ```typescript
  const res = await authFetch("/api/uploads", { method: "POST", body: form });
  ```
  (Keep each function's existing `if (!res.ok) throw` + `res.json()`/blob handling.)

- [ ] **Step 3: Continue directly into Task 6** (no build or commit yet — the four caller files still import the removed functions and the build is red until Task 6's steps are done).

---

### Task 6: Frontend — callers: callback page, garage boot, sign-out

**Files:**
- Modify: `src/frontend/src/app/auth/callback/page.tsx`
- Modify: `src/frontend/src/app/garage/layout.tsx`
- Modify: `src/frontend/src/components/Sidebar.tsx`
- Modify: `src/frontend/src/components/MobileNav.tsx`

**Interfaces:**
- Consumes: `refreshAccessToken()`, `logout()` from `lib/auth.ts` (Task 5).

- [ ] **Step 1: Callback page exchanges cookie for token**

In `app/auth/callback/page.tsx`, replace the token-param logic:

```tsx
import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { refreshAccessToken } from "@/lib/auth";

export default function AuthCallback() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  useEffect(() => {
    refreshAccessToken().then((ok) => {
      navigate(ok ? "/garage/check" : "/?error=auth_failed", { replace: true });
    });
  }, [navigate]);

  return (
    <div className="flex min-h-screen items-center justify-center bg-page">
      <p className="text-sm text-dim">{t("auth.signingIn")}</p>
    </div>
  );
}
```

- [ ] **Step 2: Garage layout boots from the cookie**

In `app/garage/layout.tsx`, replace the import of `getToken` with `refreshAccessToken` and replace the `useEffect` body:

```tsx
  useEffect(() => {
    (async () => {
      const ok = await refreshAccessToken();
      if (!ok) {
        navigate("/", { replace: true });
        return;
      }
      try {
        const data = await api.me();
        setMe(data);
        setReady(true);
      } catch {
        navigate("/", { replace: true });
      }
    })();
  }, [navigate]);
```

- [ ] **Step 3: Sign-out revokes the session**

In `components/Sidebar.tsx` and `components/MobileNav.tsx`, replace the `removeToken` import with `logout` from `@/lib/auth`, and replace each `signOut` function:

```tsx
  async function signOut() {
    await logout();
    navigate("/");
  }
```

- [ ] **Step 4: Build**

Run (from `src/frontend`): `npm run build`
Expected: PASS, no type errors. Also run `npm run lint` — no new warnings.

- [ ] **Step 5: Commit (covers Task 5 + Task 6 changes together)**

```bash
git add src/frontend
git commit -m "feat: in-memory access token, single-flight refresh, cookie session boot"
```

---

### Task 7: End-to-end verification and deploy

**Files:** none (verification only)

- [ ] **Step 1: Full local run**

From repo root: `podman compose up -d --build`
Walk the flow in a browser at `http://localhost:3000`:
1. Sign in with Google → must land on `/garage/check` **without** `?token=` ever appearing in the URL bar or history.
2. DevTools → Application → Cookies: `av_refresh` present, HttpOnly, Path=/api/auth. localStorage: no `av_token`.
3. Reload the page → still logged in (boot refresh works).
4. DevTools console: `fetch("/api/auth/refresh", {method:"POST"}).then(r=>r.status)` → 200.
5. Sign out → back on landing; reload `/garage/check` → redirected to `/`.

- [ ] **Step 2: Simulate expiry**

Temporarily set `Auth__JwtExpirationMinutes: 1` on the `auto-verdict-api` service env in `docker-compose.override.yml`, restart the API, sign in, wait >1 min, then click around the garage. Expected: requests keep working (silent refresh; check the Network tab for a `refresh` call followed by a retried request). Revert the override afterwards.

- [ ] **Step 3: Run the whole backend test suite once more**

From `src/backend`: `dotnet test`
Expected: all PASS.

- [ ] **Step 4: Deploy**

Push main, then run the auto-verdict `Deploy` workflow (PRD). After deploy, verify live:
```bash
curl -s -o /dev/null -w "%{http_code}\n" -X POST https://autoverdict.app/api/auth/refresh   # 401 (no cookie)
curl -s -o /dev/null -w "%{http_code}\n" -X POST https://autoverdict.app/api/auth/logout    # 204
curl -s -o /dev/null -w "%{http_code}\n" https://autoverdict.app/api/me                     # 401
```
Then a real browser login on https://autoverdict.app.

- [ ] **Step 5: Close out**

Mark the beads issue for auth refresh closed (if the tracker is working); update `docs/superpowers/specs/2026-07-03-auth-refresh-design.md` status line to `Implemented`.
```bash
git add -A && git commit -m "docs: mark auth refresh design implemented" && git push
```

using System.IO;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AutoVerdict.Application.Auth;
using AutoVerdict.Application.Payments;
using Microsoft.AspNetCore.HttpOverrides;
using AutoVerdict.Application.Checks;
using AutoVerdict.Application.Storage;
using AutoVerdict.Contracts.Configuration;
using AutoVerdict.Contracts.Dtos;
using AutoVerdict.Contracts.Enums;
using AutoVerdict.Contracts.Reports;
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure;
using AutoVerdict.Infrastructure.Auth;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddHealthChecks();
builder.Services.AddAntiforgery();
builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOutboxPublisher();

var authOptions = DependencyInjection.GetAuthOptions(builder.Configuration);

builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(opts =>
{
    opts.ClientId = authOptions.GoogleClientId ?? "";
    opts.ClientSecret = authOptions.GoogleClientSecret ?? "";
    opts.CallbackPath = "/api/auth/google/callback";
})
.AddJwtBearer(opts =>
{
    opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = authOptions.JwtIssuer,
        ValidateAudience = true,
        ValidAudience = authOptions.JwtAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(authOptions.JwtSecret ?? "dev-secret-placeholder-replace-in-prod")),
    };
});

builder.Services.AddAuthorization();

var testMode = string.Equals(builder.Configuration["TEST_MODE"], "true", StringComparison.OrdinalIgnoreCase);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

app.UseAuthentication();
app.UseAuthorization();

if (testMode)
{
    app.Use(async (ctx, next) =>
    {
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var email = ctx.User.FindFirst("email")?.Value
                     ?? ctx.User.FindFirst(ClaimTypes.Email)?.Value
                     ?? "";
            var whitelist = ctx.RequestServices.GetRequiredService<IOptions<WhitelistOptions>>().Value;
            if (!whitelist.Contains(email))
            {
                ctx.Response.StatusCode = 403;
                ctx.Response.ContentType = "application/problem+json";
                await ctx.Response.WriteAsJsonAsync(new
                {
                    title = "Access restricted",
                    detail = "The application is running in test mode. Your account is not permitted.",
                    status = 403,
                });
                return;
            }
        }
        await next(ctx);
    });
}

app.MapGet("/", () => Results.Ok(new { service = "AutoVerdict.Api", status = "ok" }));
app.MapHealthChecks("/health");

// ── Auth ──────────────────────────────────────────────────────────────────────

app.MapGet("/api/auth/google", (HttpContext ctx) =>
{
    var props = new AuthenticationProperties { RedirectUri = "/api/auth/google/complete" };
    return Results.Challenge(props, [GoogleDefaults.AuthenticationScheme]);
});

app.MapGet("/api/auth/google/complete", async (
    HttpContext ctx,
    IUserAuthService userAuthService,
    JwtService jwtService,
    CancellationToken ct) =>
{
    var result = await ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (!result.Succeeded)
        return Results.Unauthorized();

    var googleId = result.Principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
    var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

    if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
        return Results.Problem("Google did not return required profile claims.", statusCode: 502);

    var user = await userAuthService.FindOrCreateAsync("google", googleId, email, name, ct);

    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    var token = jwtService.GenerateToken(user.Id, user.Email);
    return Results.Redirect($"/auth/callback?token={Uri.EscapeDataString(token)}");
});

// ── Me ────────────────────────────────────────────────────────────────────────

app.MapGet("/api/me", async (
    HttpContext ctx,
    AppDbContext db,
    CancellationToken ct) =>
{
    var userId = GetUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    var user = await db.Users
        .Include(u => u.Credits)
        .Where(u => u.Id == userId.Value)
        .FirstOrDefaultAsync(ct);

    if (user is null) return Results.NotFound();

    return Results.Ok(new
    {
        id = user.Id,
        email = user.Email,
        displayName = user.DisplayName,
        credits = user.Credits?.Balance ?? 0,
    });
}).RequireAuthorization();

// ── File upload ───────────────────────────────────────────────────────────────

app.MapPost("/api/uploads", async (
    HttpContext ctx,
    IDocumentStorageClient storage,
    AppDbContext db,
    CancellationToken ct) =>
{
    var userId = GetUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    var form = await ctx.Request.ReadFormAsync(ct);
    var file = form.Files.GetFile("file");

    if (file is null)
        return Results.BadRequest("A 'file' form field is required.");

    string[] allowed = ["image/jpeg", "image/png", "image/webp", "application/pdf"];
    if (!allowed.Contains(file.ContentType))
        return Results.BadRequest(
            $"Content type '{file.ContentType}' is not supported. Allowed: {string.Join(", ", allowed)}.");

    const long maxBytes = 10L * 1024 * 1024;
    if (file.Length > maxBytes)
        return Results.BadRequest("File exceeds the 10 MB size limit.");

    string ext = file.ContentType switch
    {
        "image/jpeg" => "jpg",
        "image/png"  => "png",
        "image/webp" => "webp",
        _            => "pdf",
    };
    var fileId = Guid.NewGuid();
    var storageKey = $"uploads/{userId}/{fileId}.{ext}";

    await storage.UploadAsync(storageKey, file.OpenReadStream(), file.ContentType, ct);

    db.UploadedFiles.Add(new UploadedFile
    {
        Id = fileId,
        UserId = userId.Value,
        StorageKey = storageKey,
        ContentType = file.ContentType,
        FileSizeBytes = file.Length,
        OriginalFileName = file.FileName,
        CreatedAt = DateTimeOffset.UtcNow,
    });
    await db.SaveChangesAsync(ct);

    return Results.Ok(new FileUploadResponse(storageKey, file.ContentType, file.Length));
}).DisableAntiforgery().RequireAuthorization();

// ── Car checks ────────────────────────────────────────────────────────────────

app.MapPost("/api/checks", async (
    HttpContext ctx,
    ICarCheckService checkService,
    IDocumentStorageClient storage,
    AppDbContext db,
    CancellationToken ct) =>
{
    var userId = GetUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    var form = await ctx.Request.ReadFormAsync(ct);

    // Required description
    var description = form["description"].FirstOrDefault()?.Trim();
    if (string.IsNullOrEmpty(description))
        return Results.BadRequest("A 'description' is required.");

    // Optional otomoto URL
    var rawLink = form["link"].FirstOrDefault();
    string? listingUrl = null;
    if (!string.IsNullOrWhiteSpace(rawLink))
    {
        if (!TryNormalizeListingUrl(rawLink, out var normalized))
            return Results.BadRequest("Only otomoto.pl listing URLs are accepted.");
        listingUrl = normalized;
    }

    var reportLocale = form["reportLocale"].FirstOrDefault()?.Trim();
    if (!ReportLanguage.IsSupported(reportLocale))
        return Results.BadRequest("Unsupported report locale. Supported values: en, pl, de, uk, fr.");
    reportLocale = ReportLanguage.Resolve(reportLocale).Locale;

    // Enforce one-active-check-per-user before doing any further work
    var hasActiveCheck = await db.CarChecks
        .AnyAsync(c => c.UserId == userId.Value &&
                       (c.Status == CarCheckStatus.Pending || c.Status == CarCheckStatus.Processing), ct);
    if (hasActiveCheck)
        return Results.Conflict("You already have an analysis in progress. Wait for it to complete before starting a new one.");

    // Optional images — up to 5, each ≤ 2560 KB
    var imageFiles = form.Files
        .Where(f => f.Name.StartsWith("image", StringComparison.OrdinalIgnoreCase))
        .Take(5)
        .ToList();

    if (imageFiles.Count > 5)
        return Results.BadRequest("A maximum of 5 images may be attached.");

    const long maxImageBytes = 2560L * 1024;
    string[] allowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
    foreach (var f in imageFiles)
    {
        if (f.Length > maxImageBytes)
            return Results.BadRequest($"Image '{f.FileName}' exceeds the 2560 KB limit.");
        if (!allowedImageTypes.Contains(f.ContentType))
            return Results.BadRequest($"Image '{f.FileName}' has unsupported type '{f.ContentType}'. Allowed: JPEG, PNG, WEBP.");
    }

    var checkId = Guid.CreateVersion7();

    // Upload images into the check's dedicated folder
    var imageKeys = new List<string>(imageFiles.Count);
    foreach (var (file, index) in imageFiles.Select((f, i) => (f, i)))
    {
        string ext = file.ContentType switch
        {
            "image/jpeg" => "jpg",
            "image/png"  => "png",
            "image/webp" => "webp",
            _            => "bin",
        };
        var key = $"{checkId}/user-images/image-{index + 1}.{ext}";
        await storage.UploadAsync(key, file.OpenReadStream(), file.ContentType, ct);
        imageKeys.Add(key);
    }

    try
    {
        var check = await checkService.CreateAsync(
            userId.Value, checkId, description, listingUrl, reportLocale, [.. imageKeys], ct);

        return Results.Created($"/api/checks/{check.CheckId}", ToResponse(check));
    }
    catch (InsufficientCreditsException)
    {
        return Results.Problem(
            title: "Insufficient credits",
            detail: "You do not have enough credits to run a check.",
            statusCode: 402);
    }
}).DisableAntiforgery().RequireAuthorization();

app.MapGet("/api/checks", async (
    HttpContext ctx,
    AppDbContext db,
    CancellationToken ct,
    int page = 1,
    int pageSize = 20) =>
{
    var userId = GetUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    pageSize = Math.Clamp(pageSize, 1, 100);

    var checks = await db.CarChecks
        .Where(c => c.UserId == userId.Value)
        .OrderByDescending(c => c.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return Results.Ok(checks.Select(c => ToResponse(c)));
}).RequireAuthorization();

app.MapGet("/api/checks/{id:guid}", async (
    Guid id,
    HttpContext ctx,
    AppDbContext db,
    IDocumentStorageClient storage,
    CancellationToken ct) =>
{
    var userId = GetUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    var check = await db.CarChecks
        .Where(c => c.CheckId == id && c.UserId == userId.Value)
        .FirstOrDefaultAsync(ct);

    if (check is null)
        return Results.NotFound();

    string? report = null;
    if (check.AnalysisStorageKey is not null)
    {
        try
        {
            var (bytes, _) = await storage.DownloadAsync(check.AnalysisStorageKey, ct);
            report = Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            // Log but don't fail the request; the check may still be processing
            app.Logger.LogWarning(ex, "Could not download analysis for check {CheckId}.", id);
        }
    }

    return Results.Ok(ToResponse(check, report));
}).RequireAuthorization();

// ── Payments ──────────────────────────────────────────────────────────────────

app.MapGet("/api/payments/packages", async (
    IPaymentService paymentService,
    IMemoryCache cache,
    CancellationToken ct) =>
{
    var prices = await cache.GetOrCreateAsync("ls:package-prices", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
        return await paymentService.GetPackagePricesAsync(ct);
    });

    return Results.Ok(CreditPackage.All.Select(p =>
    {
        prices!.TryGetValue(p.Key, out var price);
        return new
        {
            key = p.Key,
            credits = p.Credits,
            label = p.Label,
            price = price?.AmountCents,
            currency = price?.Currency,
        };
    }));
});

app.MapPost("/api/payments/checkout", async (
    HttpContext ctx,
    IPaymentService paymentService,
    AppDbContext db,
    CancellationToken ct) =>
{
    var userId = GetUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    CheckoutRequest? body;
    try { body = await ctx.Request.ReadFromJsonAsync<CheckoutRequest>(ct); }
    catch { return Results.BadRequest("Invalid JSON body."); }

    if (body is null || string.IsNullOrWhiteSpace(body.Package))
        return Results.BadRequest("'package' is required.");

    var package = CreditPackage.FindByKey(body.Package);
    if (package is null)
        return Results.BadRequest($"Unknown package '{body.Package}'. Valid: credits_1, credits_3.");

    var user = await db.Users.Where(u => u.Id == userId.Value).FirstOrDefaultAsync(ct);
    if (user is null) return Results.NotFound();

    var baseUrl = GetBaseUrl(ctx);
    var successUrl = string.IsNullOrEmpty(body.SuccessUrl)
        ? $"{baseUrl}/garage/check?payment=success"
        : body.SuccessUrl;
    var cancelUrl = string.IsNullOrEmpty(body.CancelUrl)
        ? $"{baseUrl}/garage/check"
        : body.CancelUrl;

    var checkoutUrl = await paymentService.CreateCheckoutAsync(
        userId.Value, user.Email, package.Key, successUrl, cancelUrl, ct);

    return Results.Ok(new { checkoutUrl });
}).RequireAuthorization();

app.MapPost("/api/billing/webhooks/lemonsqueezy", async (
    HttpContext ctx,
    IPaymentService paymentService,
    AppDbContext db,
    CancellationToken ct) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync(ct);

    var signature = ctx.Request.Headers["X-Signature"].FirstOrDefault()
                 ?? ctx.Request.Headers["X-Lemon-Squeezy-Signature"].FirstOrDefault()
                 ?? string.Empty;

    if (!paymentService.ValidateWebhookSignature(body, signature))
        return Results.Unauthorized();

    JsonDocument doc;
    try { doc = JsonDocument.Parse(body); }
    catch { return Results.Ok(); }

    using (doc)
    {
        var root = doc.RootElement;

        if (!root.TryGetProperty("meta", out var meta)) return Results.Ok();
        if (!meta.TryGetProperty("event_name", out var eventNameProp)) return Results.Ok();
        if (eventNameProp.GetString() != "order_created") return Results.Ok();

        if (!root.TryGetProperty("data", out var data)) return Results.Ok();

        var externalOrderId = data.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        if (string.IsNullOrEmpty(externalOrderId)) return Results.Ok();

        // Only handle paid orders
        var status = data.TryGetProperty("attributes", out var attrs)
                  && attrs.TryGetProperty("status", out var statusProp)
            ? statusProp.GetString() : null;
        if (status != "paid") return Results.Ok();

        if (!meta.TryGetProperty("custom_data", out var customData)) return Results.Ok();
        if (!customData.TryGetProperty("user_id", out var userIdProp)) return Results.Ok();
        if (!customData.TryGetProperty("package", out var packageProp)) return Results.Ok();

        if (!Guid.TryParse(userIdProp.GetString(), out var userId)) return Results.Ok();
        var packageKey = packageProp.GetString();
        if (string.IsNullOrEmpty(packageKey)) return Results.Ok();

        var package = CreditPackage.FindByKey(packageKey);
        if (package is null) return Results.Ok();

        // Idempotency check
        var alreadyProcessed = await db.PaymentOrders
            .AnyAsync(o => o.ExternalOrderId == externalOrderId, ct);
        if (alreadyProcessed) return Results.Ok();

        var now = DateTimeOffset.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE user_credits
            SET "Balance" = "Balance" + {package.Credits}, "UpdatedAt" = NOW()
            WHERE "UserId" = {userId}
            """,
            ct);

        db.CreditLedgerEntries.Add(new CreditLedgerEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = package.Credits,
            Reason = "credit_purchase",
            CreatedAt = now,
        });

        db.PaymentOrders.Add(new PaymentOrder
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PackageKey = packageKey,
            CreditsGranted = package.Credits,
            ExternalOrderId = externalOrderId,
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    return Results.Ok();
}).DisableAntiforgery();

// Dev-only: simulate a completed payment without Lemon Squeezy
app.MapGet("/api/payments/mock-checkout", async (
    Guid userId,
    string package,
    string? successUrl,
    IOptions<PaymentOptions> paymentOpts,
    AppDbContext db,
    CancellationToken ct) =>
{
    if (!paymentOpts.Value.Provider.Equals("mock", StringComparison.OrdinalIgnoreCase))
        return Results.NotFound();

    var pkg = CreditPackage.FindByKey(package);
    if (pkg is null) return Results.BadRequest("Unknown package.");

    var now = DateTimeOffset.UtcNow;
    var externalOrderId = $"mock-{Guid.NewGuid():N}";

    await db.Database.ExecuteSqlAsync(
        $"""
        UPDATE user_credits
        SET "Balance" = "Balance" + {pkg.Credits}, "UpdatedAt" = NOW()
        WHERE "UserId" = {userId}
        """,
        ct);

    db.CreditLedgerEntries.Add(new CreditLedgerEntry
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Amount = pkg.Credits,
        Reason = "credit_purchase",
        CreatedAt = now,
    });

    db.PaymentOrders.Add(new PaymentOrder
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        PackageKey = package,
        CreditsGranted = pkg.Credits,
        ExternalOrderId = externalOrderId,
        CreatedAt = now,
    });

    await db.SaveChangesAsync(ct);

    var redirect = string.IsNullOrEmpty(successUrl) ? "/garage/check?payment=success" : successUrl;
    return Results.Redirect(redirect);
}).DisableAntiforgery();

app.Run();

static CarCheckResponse ToResponse(CarCheck check, string? report = null) =>
    new(
        check.CheckId,
        check.Title,
        check.ListingUrl,
        check.Status,
        report,
        check.FailureReason,
        check.CreatedAt,
        check.Status is CarCheckStatus.Completed or CarCheckStatus.Failed ? check.UpdatedAt : null);

static bool TryNormalizeListingUrl(string? rawUrl, out string normalized)
{
    normalized = "";
    if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
        return false;

    if (uri.Scheme is not ("http" or "https"))
        return false;

    string host = uri.IdnHost.ToLowerInvariant();
    if (host != "otomoto.pl" && !host.EndsWith(".otomoto.pl", StringComparison.Ordinal))
        return false;

    var builder = new UriBuilder(uri) { Fragment = "" };
    normalized = builder.Uri.ToString();
    return true;
}

static Guid? GetUserId(HttpContext ctx)
{
    var sub = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
           ?? ctx.User.FindFirst("sub")?.Value;
    return Guid.TryParse(sub, out var id) ? id : null;
}

static string GetBaseUrl(HttpContext ctx)
{
    var proto = ctx.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? ctx.Request.Scheme;
    var host = ctx.Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? ctx.Request.Host.ToString();
    return $"{proto}://{host}";
}

record CheckoutRequest(string Package, string? SuccessUrl, string? CancelUrl);

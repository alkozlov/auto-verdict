using System.Security.Claims;
using System.Text;
using AutoVerdict.Application.Auth;
using Microsoft.AspNetCore.HttpOverrides;
using AutoVerdict.Application.Checks;
using AutoVerdict.Application.Storage;
using AutoVerdict.Contracts.Dtos;
using AutoVerdict.Contracts.Enums;
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure;
using AutoVerdict.Infrastructure.Auth;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddHealthChecks();
builder.Services.AddAntiforgery();
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

var app = builder.Build();

// Apply EF Core migrations on every startup so the DB schema is always current.
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
    [FromBody] CarCheckCreateRequest request,
    ICarCheckService checkService,
    CancellationToken ct) =>
{
    var userId = GetUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(request.VehicleIdentifier) ||
        string.IsNullOrWhiteSpace(request.DocumentStorageKey))
        return Results.BadRequest("VehicleIdentifier and DocumentStorageKey are required.");

    try
    {
        var check = await checkService.CreateAsync(
            userId.Value, request.VehicleIdentifier, request.DocumentStorageKey, ct);

        return Results.Created(
            $"/api/checks/{check.Id}",
            new CarCheckResponse(check.Id, check.VehicleIdentifier, check.Status,
                Report: null, FailureReason: null, check.CreatedAt, CompletedAt: null));
    }
    catch (InsufficientCreditsException)
    {
        return Results.Problem(
            title: "Insufficient credits",
            detail: "You do not have enough credits to run a check.",
            statusCode: 402);
    }
}).RequireAuthorization();

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
        .Include(c => c.Report)
        .Where(c => c.UserId == userId.Value)
        .OrderByDescending(c => c.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    var items = checks.Select(c => new CarCheckResponse(
        c.Id, c.VehicleIdentifier, c.Status,
        c.Report?.ReportData, c.FailureReason, c.CreatedAt,
        c.Status is CarCheckStatus.Completed or CarCheckStatus.Failed ? c.UpdatedAt : null));

    return Results.Ok(items);
}).RequireAuthorization();

app.MapGet("/api/checks/{id:guid}", async (
    Guid id,
    HttpContext ctx,
    AppDbContext db,
    CancellationToken ct) =>
{
    var userId = GetUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    var check = await db.CarChecks
        .Include(c => c.Report)
        .Where(c => c.Id == id && c.UserId == userId.Value)
        .FirstOrDefaultAsync(ct);

    if (check is null)
        return Results.NotFound();

    return Results.Ok(new CarCheckResponse(
        check.Id, check.VehicleIdentifier, check.Status,
        check.Report?.ReportData, check.FailureReason, check.CreatedAt,
        check.Status is CarCheckStatus.Completed or CarCheckStatus.Failed ? check.UpdatedAt : null));
}).RequireAuthorization();

app.Run();

static Guid? GetUserId(HttpContext ctx)
{
    var sub = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
           ?? ctx.User.FindFirst("sub")?.Value;
    return Guid.TryParse(sub, out var id) ? id : null;
}

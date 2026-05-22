using AutoVerdict.Application.Checks;
using AutoVerdict.Application.Storage;
using AutoVerdict.Contracts.Dtos;
using AutoVerdict.Contracts.Enums;
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddHealthChecks();
builder.Services.AddAntiforgery();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { service = "AutoVerdict.Api", status = "ok" }));
app.MapHealthChecks("/health");

// ── File upload ───────────────────────────────────────────────────────────────
// TODO av-011: replace X-User-Id header with real JWT auth

app.MapPost("/api/uploads", async (
    HttpContext ctx,
    [FromHeader(Name = "X-User-Id")] Guid? userId,
    IDocumentStorageClient storage,
    AppDbContext db,
    CancellationToken ct) =>
{
    if (userId is null)
        return Results.Unauthorized();

    var form = await ctx.Request.ReadFormAsync(ct);
    var file = form.Files.GetFile("file");

    if (file is null)
        return Results.BadRequest("A 'file' form field is required.");

    string[] allowed = ["image/jpeg", "image/png", "image/webp", "application/pdf"];
    if (!allowed.Contains(file.ContentType))
        return Results.BadRequest(
            $"Content type '{file.ContentType}' is not supported. Allowed: {string.Join(", ", allowed)}.");

    const long maxBytes = 10L * 1024 * 1024; // 10 MB
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
}).DisableAntiforgery();

// ── Car checks ────────────────────────────────────────────────────────────────

app.MapPost("/api/checks", async (
    [FromHeader(Name = "X-User-Id")] Guid? userId,
    [FromBody] CarCheckCreateRequest request,
    ICarCheckService checkService,
    CancellationToken ct) =>
{
    if (userId is null)
        return Results.Unauthorized();

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
});

app.MapGet("/api/checks", async (
    [FromHeader(Name = "X-User-Id")] Guid? userId,
    AppDbContext db,
    CancellationToken ct,
    int page = 1,
    int pageSize = 20) =>
{
    if (userId is null)
        return Results.Unauthorized();

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
});

app.MapGet("/api/checks/{id:guid}", async (
    Guid id,
    [FromHeader(Name = "X-User-Id")] Guid? userId,
    AppDbContext db,
    CancellationToken ct) =>
{
    if (userId is null)
        return Results.Unauthorized();

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
});

app.Run();

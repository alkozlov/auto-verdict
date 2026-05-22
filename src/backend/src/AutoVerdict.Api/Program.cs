using AutoVerdict.Application.Checks;
using AutoVerdict.Contracts.Dtos;
using AutoVerdict.Contracts.Enums;
using AutoVerdict.Infrastructure;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { service = "AutoVerdict.Api", status = "ok" }));
app.MapHealthChecks("/health");

// TODO av-011: replace X-User-Id header with real JWT auth
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
            userId.Value,
            request.VehicleIdentifier,
            request.DocumentStorageKey,
            ct);

        var response = new CarCheckResponse(
            check.Id,
            check.VehicleIdentifier,
            check.Status,
            Report: null,
            FailureReason: null,
            check.CreatedAt,
            CompletedAt: null);

        return Results.Created($"/api/checks/{check.Id}", response);
    }
    catch (InsufficientCreditsException)
    {
        return Results.Problem(
            title: "Insufficient credits",
            detail: "You do not have enough credits to run a check.",
            statusCode: 402);
    }
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
        .Where(c => c.Id == id && c.UserId == userId.Value)
        .FirstOrDefaultAsync(ct);

    if (check is null)
        return Results.NotFound();

    var response = new CarCheckResponse(
        check.Id,
        check.VehicleIdentifier,
        check.Status,
        Report: null,
        FailureReason: check.FailureReason,
        check.CreatedAt,
        CompletedAt: check.Status is CarCheckStatus.Completed or CarCheckStatus.Failed
            ? check.UpdatedAt
            : null);

    return Results.Ok(response);
});

app.Run();

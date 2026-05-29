using AutoVerdict.Application.AI;
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure.AI;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class AiStageRunner(
    IAiClient aiClient,
    IServiceScopeFactory scopeFactory,
    IOptions<AiPricingOptions> pricingOptions,
    ILogger<AiStageRunner> logger)
{
    private readonly AiPricingOptions _pricingOptions = pricingOptions.Value;

    public async Task<AiTextResponse> RunAsync(
        AiTextRequest request,
        AiBudgetTracker budget,
        string? escalationReason = null,
        string? validationWarningsJson = null,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            var response = await aiClient.CreateTextAsync(request, cancellationToken);
            var estimatedCost = EstimateCostEur(response.Model, response.InputTokens, response.OutputTokens);
            budget.Add(estimatedCost);

            await RecordRunAsync(
                request,
                response,
                estimatedCost,
                "Succeeded",
                null,
                escalationReason,
                validationWarningsJson,
                startedAt,
                cancellationToken);

            logger.LogInformation(
                "AI stage {Stage} completed for check {CheckId}: model={Model}, tokens={InputTokens}+{OutputTokens}, estimatedCostEur={Cost}.",
                request.Stage, request.CheckId, response.Model, response.InputTokens, response.OutputTokens, estimatedCost);

            return response;
        }
        catch (Exception ex)
        {
            await RecordFailedRunAsync(
                request,
                ex,
                escalationReason,
                validationWarningsJson,
                startedAt,
                cancellationToken);

            logger.LogError(
                ex,
                "AI stage {Stage} failed for check {CheckId} using model {Model}.",
                request.Stage,
                request.CheckId,
                request.Model);

            throw;
        }
    }

    public decimal EstimateMaxCostEur(string model, int maxInputTokens, int maxOutputTokens) =>
        EstimateCostEur(model, maxInputTokens, maxOutputTokens);

    private decimal EstimateCostEur(string model, long inputTokens, long outputTokens)
    {
        var pricing = _pricingOptions.GetModel(model);
        var usd =
            (inputTokens / 1_000_000m * pricing.InputPerMillionTokensUsd) +
            (outputTokens / 1_000_000m * pricing.OutputPerMillionTokensUsd);

        return Math.Round(usd * _pricingOptions.UsdToEurRate, 6, MidpointRounding.AwayFromZero);
    }

    private async Task RecordRunAsync(
        AiTextRequest request,
        AiTextResponse response,
        decimal estimatedCost,
        string status,
        string? errorMessage,
        string? escalationReason,
        string? validationWarningsJson,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AiRuns.Add(new AiRun
        {
            CheckId = request.CheckId,
            Stage = request.Stage,
            Provider = response.Provider,
            Model = response.Model,
            PromptVersion = request.PromptVersion,
            InputTokens = response.InputTokens,
            OutputTokens = response.OutputTokens,
            EstimatedCostEur = estimatedCost,
            DurationMs = (long)response.Duration.TotalMilliseconds,
            Status = status,
            ErrorMessage = errorMessage,
            EscalationReason = escalationReason,
            ValidationWarningsJson = validationWarningsJson,
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = startedAt,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordFailedRunAsync(
        AiTextRequest request,
        Exception exception,
        string? escalationReason,
        string? validationWarningsJson,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AiRuns.Add(new AiRun
        {
            CheckId = request.CheckId,
            Stage = request.Stage,
            Provider = "Claude",
            Model = request.Model,
            PromptVersion = request.PromptVersion,
            InputTokens = 0,
            OutputTokens = 0,
            EstimatedCostEur = 0,
            DurationMs = (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds,
            Status = "Failed",
            ErrorMessage = exception.Message.Length <= 2000 ? exception.Message : exception.Message[..2000],
            EscalationReason = escalationReason,
            ValidationWarningsJson = validationWarningsJson,
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = startedAt,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}

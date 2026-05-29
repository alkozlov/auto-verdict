using System.Diagnostics;
using System.Diagnostics.Metrics;
using AutoVerdict.Application.AI;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class AiPipelineMetrics
{
    public const string MeterName = "AutoVerdict.ProcessingService.AI";

    private readonly Counter<long> _tokens;
    private readonly Counter<double> _estimatedCost;
    private readonly Counter<long> _requests;
    private readonly Histogram<double> _duration;

    public AiPipelineMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _tokens = meter.CreateCounter<long>(
            "autoverdict_ai_tokens_total",
            unit: "tokens",
            description: "AI token usage by provider, model, pipeline stage, and token type.");
        _estimatedCost = meter.CreateCounter<double>(
            "autoverdict_ai_estimated_cost_eur_total",
            unit: "EUR",
            description: "Estimated cumulative AI cost in EUR by provider, model, and pipeline stage.");
        _requests = meter.CreateCounter<long>(
            "autoverdict_ai_requests_total",
            unit: "requests",
            description: "AI request count by provider, model, pipeline stage, and status.");
        _duration = meter.CreateHistogram<double>(
            "autoverdict_ai_request_duration_ms",
            unit: "ms",
            description: "AI request duration by provider, model, pipeline stage, and status.");
    }

    public void RecordSuccess(
        AiTextRequest request,
        AiTextResponse response,
        decimal estimatedCostEur)
    {
        var tags = BuildTags(request.Stage, response.Provider, response.Model, "succeeded");

        _requests.Add(1, tags);
        _duration.Record(response.Duration.TotalMilliseconds, tags);
        _estimatedCost.Add((double)estimatedCostEur, tags);

        _tokens.Add(response.InputTokens, AddTokenType(tags, "input"));
        _tokens.Add(response.OutputTokens, AddTokenType(tags, "output"));
        _tokens.Add(response.InputTokens + response.OutputTokens, AddTokenType(tags, "total"));
    }

    public void RecordFailure(AiTextRequest request, TimeSpan duration)
    {
        var tags = BuildTags(request.Stage, "Claude", request.Model, "failed");
        _requests.Add(1, tags);
        _duration.Record(duration.TotalMilliseconds, tags);
    }

    private static TagList BuildTags(
        string stage,
        string provider,
        string model,
        string status)
    {
        var tags = new TagList
        {
            { "ai.provider", provider },
            { "ai.model", model },
            { "ai.stage", stage },
            { "ai.request.status", status },
        };

        return tags;
    }

    private static TagList AddTokenType(TagList source, string tokenType)
    {
        var tags = source;
        tags.Add("ai.token.type", tokenType);
        return tags;
    }
}

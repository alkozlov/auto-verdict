using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using AutoVerdict.Application.AI;
using AutoVerdict.Contracts.Report;
using Microsoft.Extensions.Options;

namespace AutoVerdict.Infrastructure.AI;

public sealed class ClaudeAiAnalysisProvider : IAiAnalysisProvider
{
    private const string ProviderName = "Claude";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Dictionary<string, JsonElement> ReportSchema = BuildReportSchema();

    private readonly AnthropicClient _client;
    private readonly ClaudeOptions _options;

    public ClaudeAiAnalysisProvider(IOptions<ClaudeOptions> options)
    {
        _options = options.Value;
        _client = new AnthropicClient { ApiKey = _options.ApiKey };
    }

    public async Task<AiAnalysisResult> AnalyzeAsync(AiAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            Thinking = new ThinkingConfigAdaptive(),
            OutputConfig = new OutputConfig
            {
                Format = new JsonOutputFormat { Schema = ReportSchema },
            },
            System = BuildSystemPrompt(),
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    Content = BuildUserPrompt(request),
                },
            ],
        };

        Message response = await _client.Messages.Create(parameters, cancellationToken: cancellationToken);

        string json = response.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .Select(t => t.Text)
            .FirstOrDefault() ?? throw new InvalidOperationException("Claude returned no text content.");

        VehicleReport report = JsonSerializer.Deserialize<VehicleReport>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize VehicleReport from Claude response.");

        return new AiAnalysisResult(
            report,
            ProviderName,
            _options.Model,
            response.Usage.InputTokens,
            response.Usage.OutputTokens);
    }

    private static string BuildSystemPrompt() =>
        """
        You are a vehicle history analyst. Given vehicle document data, produce a structured
        vehicle history report. Be factual and conservative — only report what the data supports.
        Respond with valid JSON matching the provided schema exactly.
        """;

    private static string BuildUserPrompt(AiAnalysisRequest request) =>
        $"""
        Analyze the following vehicle document for vehicle identifier "{request.VehicleIdentifier}".

        Document:
        {request.DocumentContent}
        """;

    private static Dictionary<string, JsonElement> BuildReportSchema()
    {
        string schemaJson = """
            {
              "type": "object",
              "required": ["VehicleIdentifier","Verdict","Ownership","Mileage","Accidents","Service","Legal"],
              "properties": {
                "VehicleIdentifier": { "type": "string" },
                "Verdict": { "type": "string" },
                "Ownership": {
                  "type": "object",
                  "required": ["OwnersCount","CommercialUseDetected"],
                  "properties": {
                    "OwnersCount": { "type": "integer" },
                    "CommercialUseDetected": { "type": "boolean" },
                    "Notes": { "type": ["string","null"] }
                  }
                },
                "Mileage": {
                  "type": "object",
                  "required": ["InconsistencyDetected"],
                  "properties": {
                    "InconsistencyDetected": { "type": "boolean" },
                    "LastRecordedKm": { "type": ["integer","null"] },
                    "Notes": { "type": ["string","null"] }
                  }
                },
                "Accidents": {
                  "type": "object",
                  "required": ["TotalCount","SevereDamageDetected"],
                  "properties": {
                    "TotalCount": { "type": "integer" },
                    "SevereDamageDetected": { "type": "boolean" },
                    "Notes": { "type": ["string","null"] }
                  }
                },
                "Service": {
                  "type": "object",
                  "required": ["RegularMaintenanceConfirmed"],
                  "properties": {
                    "RegularMaintenanceConfirmed": { "type": "boolean" },
                    "LastServiceDate": { "type": ["string","null"], "format": "date-time" },
                    "Notes": { "type": ["string","null"] }
                  }
                },
                "Legal": {
                  "type": "object",
                  "required": ["PledgeDetected","StolenDetected","WantedDetected"],
                  "properties": {
                    "PledgeDetected": { "type": "boolean" },
                    "StolenDetected": { "type": "boolean" },
                    "WantedDetected": { "type": "boolean" },
                    "Notes": { "type": ["string","null"] }
                  }
                }
              }
            }
            """;

        using JsonDocument doc = JsonDocument.Parse(schemaJson);
        // Materialize all nodes so we don't hold a reference to a disposed JsonDocument
        return doc.RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());
    }
}

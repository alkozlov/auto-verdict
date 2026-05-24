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
                    Content = BuildUserContent(request),
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
            request.Listing,
            ProviderName,
            _options.Model,
            response.Usage.InputTokens,
            response.Usage.OutputTokens);
    }

    private static string BuildSystemPrompt() =>
        """
        You analyze used-car listings in Poland for inexperienced private buyers.
        Be conservative and practical. Distinguish model/technical risks from deal,
        seller, wording, missing-information, and transaction risks. Do not make
        unsupported claims. Use "unknown" or explain uncertainty when the listing
        data is insufficient. Return valid JSON matching the schema exactly.
        """;

    private static List<ContentBlockParam> BuildUserContent(AiAnalysisRequest request)
    {
        var listingJson = JsonSerializer.Serialize(request.Listing, JsonOptions);
        var prompt =
            $"""
            Analyze this Otomoto listing for a family buying a relatively recent used car in Poland.

            Goals:
            1. Identify practical ownership and model risks.
            2. Identify purchase/sale transaction risks, inconsistencies, omissions, and seller questions.
            3. Estimate final private-buyer costs in Poland using conservative assumptions.
            4. Give a clear recommendation and checklist for follow-up.

            Extracted listing data:
            {listingJson}
            """;

        return
        [
            new TextBlockParam { Text = prompt },
            new ImageBlockParam
            {
                Source = new Base64ImageSource
                {
                    Data = Convert.ToBase64String(request.ScreenshotBytes),
                    MediaType = request.ScreenshotContentType,
                },
            },
        ];
    }

    private static Dictionary<string, JsonElement> BuildReportSchema()
    {
        const string schemaJson = """
            {
              "type": "object",
              "additionalProperties": false,
              "required": [
                "CarSummary",
                "ListingFacts",
                "ModelRisks",
                "ListingRisks",
                "DealRisks",
                "EstimatedCosts",
                "SellerQuestions",
                "InspectionChecklist",
                "Recommendation",
                "Disclaimer"
              ],
              "properties": {
                "CarSummary": { "type": "string" },
                "ListingFacts": {
                  "type": "object",
                  "additionalProperties": false,
                  "required": ["ListingUrl"],
                  "properties": {
                    "ListingUrl": { "type": "string" },
                    "Title": { "type": ["string", "null"] },
                    "Make": { "type": ["string", "null"] },
                    "Model": { "type": ["string", "null"] },
                    "Year": { "type": ["integer", "null"] },
                    "MileageKm": { "type": ["integer", "null"] },
                    "Price": { "type": ["number", "null"] },
                    "Currency": { "type": ["string", "null"] },
                    "SellerType": { "type": ["string", "null"] },
                    "Location": { "type": ["string", "null"] }
                  }
                },
                "ModelRisks": { "type": "array", "items": { "type": "string" } },
                "ListingRisks": { "type": "array", "items": { "type": "string" } },
                "DealRisks": { "type": "array", "items": { "type": "string" } },
                "EstimatedCosts": {
                  "type": "object",
                  "additionalProperties": false,
                  "required": ["Currency", "Notes"],
                  "properties": {
                    "PurchasePrice": { "type": ["number", "null"] },
                    "RegistrationFee": { "type": ["number", "null"] },
                    "InsuranceCost": { "type": ["number", "null"] },
                    "PotentialRepairs": { "type": ["number", "null"] },
                    "Total": { "type": ["number", "null"] },
                    "Currency": { "type": "string" },
                    "Notes": { "type": "string" }
                  }
                },
                "SellerQuestions": { "type": "array", "items": { "type": "string" } },
                "InspectionChecklist": { "type": "array", "items": { "type": "string" } },
                "Recommendation": { "type": "string" },
                "Disclaimer": { "type": "string" }
              }
            }
            """;

        using JsonDocument doc = JsonDocument.Parse(schemaJson);
        return doc.RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());
    }
}

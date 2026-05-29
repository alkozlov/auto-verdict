using AutoVerdict.Application.AI;
using AutoVerdict.Infrastructure.AI;
using Microsoft.Extensions.Options;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class RiskAnalysisStage(
    AiStageRunner runner,
    EvidenceFormatter formatter,
    IOptions<AiPipelineOptions> options)
{
    private const string StageName = "RiskAnalysis";
    private const string PromptVersion = "risk-analysis.v1";

    private readonly AiPipelineOptions _options = options.Value;

    public async Task<RiskAnalysisResult> ExecuteAsync(
        EvidenceBundle evidence,
        ExtractedVehicleFacts facts,
        AiBudgetTracker budget,
        CancellationToken cancellationToken)
    {
        var stage = _options.GetStage(StageName, "claude-sonnet-4-6", 5000);
        var response = await runner.RunAsync(
            new AiTextRequest(
                evidence.CheckId,
                StageName,
                stage.Model,
                PromptVersion,
                BuildSystemPrompt(),
                [
                    new AiTextContent(
                        $$"""
                        Analyze used-car purchase risks for a private buyer in Poland.

                        Return ONLY valid JSON matching this shape:
                        {
                          "overallRiskLevel": "low" | "medium" | "high" | "unknown",
                          "recommendedVerdict": "Buy" | "Buy with caution" | "Avoid",
                          "confidence": "low" | "medium" | "high",
                          "technicalRisks": [{ "severity": string, "title": string, "explanation": string, "source": string, "howToVerify": string }],
                          "listingRisks": [{ "severity": string, "title": string, "explanation": string, "source": string, "howToVerify": string }],
                          "dealRisks": [{ "severity": string, "title": string, "explanation": string, "source": string, "howToVerify": string }],
                          "missingInformation": [string],
                          "sellerQuestions": [string],
                          "inspectionChecklist": [string],
                          "costAssumptions": [string],
                          "inconsistencies": [string],
                          "needsEscalation": boolean,
                          "escalationReason": string | null
                        }

                        Rules:
                        - Be cautious and practical.
                        - Do not accuse the seller of fraud.
                        - Do not claim certainty without evidence.
                        - If crawler data is unavailable, explicitly treat user text/images as the available evidence.
                        - Do not infer listing facts from the URL alone.
                        - Model-specific risks must be verification points, not definitive defects.
                        - Include only useful questions and checklist items.
                        - Use PLN assumptions for costs.
                        - Set needsEscalation=true only if a stronger model review is likely to materially improve the report.

                        Extracted facts:
                        {{formatter.BuildFactsText(facts)}}

                        Evidence summary:
                        {{formatter.BuildEvidenceText(evidence)}}
                        """),
                ],
                stage.MaxTokens),
            budget,
            cancellationToken: cancellationToken);

        return AiJson.DeserializeFromModel<RiskAnalysisResult>(response.Text);
    }

    private static string BuildSystemPrompt() =>
        """
        You are AutoVerdict's risk analysis component.
        Your job is to identify purchase risks, missing information, inconsistencies,
        seller questions, and inspection points for a used-car buyer.
        Return strict JSON only. Do not write the final report.
        """;
}

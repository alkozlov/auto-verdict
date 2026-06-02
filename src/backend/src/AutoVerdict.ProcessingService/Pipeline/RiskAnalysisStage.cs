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
        bool isFreeReview,
        CancellationToken cancellationToken)
    {
        var stage = _options.GetStage(StageName, "claude-sonnet-4-6", 5000);
        var model = isFreeReview ? "claude-haiku-4-5" : stage.Model;
        var response = await runner.RunAsync(
            new AiTextRequest(
                evidence.CheckId,
                StageName,
                model,
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
                          "technicalRisks": [{ "severity": string, "title": string, "explanation": string, "source": string, "howToVerify": string, "evidenceStrength": string }],
                          "listingRisks": [{ "severity": string, "title": string, "explanation": string, "source": string, "howToVerify": string, "evidenceStrength": string }],
                          "dealRisks": [{ "severity": string, "title": string, "explanation": string, "source": string, "howToVerify": string, "evidenceStrength": string }],
                          "missingInformation": [string],
                          "sellerQuestions": [string],
                          "inspectionChecklist": [string],
                          "costAssumptions": [string],
                          "inconsistencies": [string],
                          "needsEscalation": boolean,
                          "escalationReason": string | null,
                          "mainConcern": string | null,
                          "recommendedNextStep": string,
                          "userQuestions": [{ "question": string, "answer": string, "status": "answered" | "partially_answered" | "not_enough_data" | "out_of_scope" }]
                        }

                        Rules:
                        - Be cautious and practical.
                        - Do not accuse the seller of fraud.
                        - Do not claim certainty without evidence.
                        - Risk item severity must be one of: low, medium, high, unknown.
                        - Risk item evidenceStrength must be one of: weak, medium, strong.
                        - mainConcern must be the single biggest issue to verify, or null if the evidence is too thin.
                        - recommendedNextStep must be one concrete buyer action.
                        - If crawler data is unavailable, explicitly treat user text/images as the available evidence.
                        - Do not infer listing facts from the URL alone.
                        - Model-specific risks must be verification points, not definitive defects.
                        - Include only useful questions and checklist items.
                        - missingInformation, sellerQuestions, inspectionChecklist, costAssumptions, and inconsistencies must be arrays of plain strings.
                        - Use PLN assumptions for costs.
                        - Set needsEscalation=true only if a stronger model review is likely to materially improve the report.
                        - Inspect the user input for explicit buyer questions.
                        - Add only questions clearly present in the user input to userQuestions; do not invent questions.
                        - Answer car-purchase-related questions briefly and practically.
                        - If evidence is insufficient, set status to not_enough_data or partially_answered and say what is needed.
                        - If a question is unrelated to car purchase analysis, include it with status out_of_scope and explain that AutoVerdict only covers purchase risk analysis.

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

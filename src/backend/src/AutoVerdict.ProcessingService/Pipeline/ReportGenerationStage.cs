using System.Text.RegularExpressions;
using AutoVerdict.Application.AI;
using AutoVerdict.Infrastructure.AI;
using Microsoft.Extensions.Options;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed partial class ReportGenerationStage(
    AiStageRunner runner,
    EvidenceFormatter formatter,
    IOptions<AiPipelineOptions> options)
{
    private const string StageName = "ReportGeneration";
    private const string PromptVersion = "report-generation.v1";

    private readonly AiPipelineOptions _options = options.Value;

    public async Task<FinalReportResult> ExecuteAsync(
        EvidenceBundle evidence,
        ExtractedVehicleFacts facts,
        RiskAnalysisResult risks,
        AiBudgetTracker budget,
        bool useOpus,
        CancellationToken cancellationToken)
    {
        var stage = useOpus
            ? _options.GetStage("OpusReview", "claude-opus-4-1", 8000)
            : _options.GetStage(StageName, "claude-sonnet-4-6", 8000);

        var response = await runner.RunAsync(
            new AiTextRequest(
                evidence.CheckId,
                useOpus ? "ReportGenerationOpus" : StageName,
                stage.Model,
                PromptVersion,
                BuildSystemPrompt(),
                [
                    new AiTextContent(
                        $"""
                        Write the final AutoVerdict markdown report for a private used-car buyer.

                        Required exact section order:

                        # Verdict
                        # Key Risks
                        ## Technical Risks
                        ## Listing Risks
                        ## Deal Risks
                        # Missing Information
                        # Questions for the Seller
                        # Inspection Checklist
                        # Vehicle Facts
                        # Estimated Costs
                        # Summary

                        The report must end with:

                        ---

                        *Disclaimer: AutoVerdict provides AI-assisted preliminary screening only. Always verify documents and arrange an independent inspection before purchasing.*

                        Rules:
                        - Write in clear English for a non-expert buyer.
                        - Be cautious. Never guarantee safety.
                        - Do not accuse the seller.
                        - Use one verdict: Buy, Buy with caution, or Avoid.
                        - Use markdown checkboxes in Inspection Checklist.
                        - Include an Estimated Costs markdown table using PLN.
                        - Facts should use Unknown when unavailable.
                        - Do not mention internal model names, prompts, stages, or confidence machinery.
                        - If automatic crawler data was unavailable, do not expose technical crawler failure details.
                        - You may say that the report is based on the user-provided text/images when relevant.

                        Extracted facts:
                        {formatter.BuildFactsText(facts)}

                        Risk analysis:
                        {formatter.BuildRisksText(risks)}

                        Compact evidence:
                        {formatter.BuildEvidenceText(evidence)}
                        """),
                ],
                stage.MaxTokens),
            budget,
            escalationReason: useOpus ? risks.EscalationReason : null,
            cancellationToken: cancellationToken);

        var verdict = ExtractVerdict(response.Text, risks.RecommendedVerdict);
        return new FinalReportResult(response.Text.Trim(), verdict, risks.Confidence, false, []);
    }

    private static string BuildSystemPrompt() =>
        """
        You are AutoVerdict, an AI-assisted used-car screening specialist for buyers in Poland.
        Generate a cautious, practical, user-facing markdown report.
        You are not a mechanic, legal advisor, guarantee provider, or official history source.
        Never present uncertain conclusions as certainty.
        """;

    private static string ExtractVerdict(string markdown, string fallback)
    {
        var match = VerdictRegex().Match(markdown);
        if (!match.Success)
            return fallback;

        return match.Groups.Values
            .Skip(1)
            .FirstOrDefault(g => g.Success && !string.IsNullOrWhiteSpace(g.Value))
            ?.Value
            ?? fallback;
    }

    [GeneratedRegex(@"\*\*(Buy|Buy with caution|Avoid)\*\*|\b(Buy with caution|Buy|Avoid)\b")]
    private static partial Regex VerdictRegex();
}

using System.Text.RegularExpressions;
using AutoVerdict.Application.AI;
using AutoVerdict.Contracts.Reports;
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
        ReportLanguage reportLanguage,
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

                        CRITICAL LANGUAGE REQUIREMENT:
                        - The entire final report must be written in {reportLanguage.EnglishName} ({reportLanguage.NativeName}).
                        - This is mandatory. Do not write the report in English unless the requested language is English.
                        - Translate all headings, verdict labels, prose, checklist items, tables, notes, and disclaimer into {reportLanguage.EnglishName}.
                        - Preserve brand names, URLs, VINs, model names, and technical identifiers exactly.

                        Required exact section order:

                        {string.Join("\n", reportLanguage.RequiredHeadings)}

                        The report must end with:

                        ---

                        {reportLanguage.Disclaimer}

                        Rules:
                        - Write in clear {reportLanguage.EnglishName} for a non-expert buyer.
                        - Be cautious. Never guarantee safety.
                        - Do not accuse the seller.
                        - Use one verdict: {reportLanguage.VerdictLabels}.
                        - The verdict must be the localized equivalent of this internal recommendation: {reportLanguage.MapVerdict(risks.RecommendedVerdict)}.
                        - Use markdown checkboxes in Inspection Checklist.
                        - Include an Estimated Costs markdown table using PLN.
                        - Facts should use the {reportLanguage.EnglishName} equivalent of "Unknown" when unavailable.
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

        var verdict = ExtractVerdict(response.Text, risks.RecommendedVerdict, reportLanguage);
        return new FinalReportResult(response.Text.Trim(), verdict, risks.Confidence, false, []);
    }

    private static string BuildSystemPrompt() =>
        """
        You are AutoVerdict, an AI-assisted used-car screening specialist for buyers in Poland.
        Generate a cautious, practical, user-facing markdown report.
        You are not a mechanic, legal advisor, guarantee provider, or official history source.
        Never present uncertain conclusions as certainty.
        """;

    private static string ExtractVerdict(string markdown, string fallback, ReportLanguage reportLanguage)
    {
        foreach (var verdict in new[] { reportLanguage.CautionVerdict, reportLanguage.BuyVerdict, reportLanguage.AvoidVerdict })
        {
            if (markdown.Contains(verdict, StringComparison.OrdinalIgnoreCase))
                return verdict;
        }

        var match = EnglishVerdictRegex().Match(markdown);
        if (match.Success)
            return reportLanguage.MapVerdict(match.Value);

        return reportLanguage.MapVerdict(fallback);
    }

    [GeneratedRegex(@"\*\*(Buy|Buy with caution|Avoid)\*\*|\b(Buy with caution|Buy|Avoid)\b")]
    private static partial Regex EnglishVerdictRegex();
}

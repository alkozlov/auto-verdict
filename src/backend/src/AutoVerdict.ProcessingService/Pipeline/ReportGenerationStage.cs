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
                        - The report must feel like a polished SaaS buyer report, not a generic AI essay.
                        - Use short paragraphs, concise tables, grouped bullets, and clear action points.
                        - Avoid long uninterrupted prose and generic filler.
                        - Be practical and decision-oriented for a non-expert private buyer.
                        - Write in clear {reportLanguage.EnglishName} for a non-expert buyer.
                        - Be cautious. Never guarantee safety.
                        - Do not accuse the seller.
                        - Never use raw HTML, code fences, internal model names, prompt/stage names, or validator details.
                        - Clearly separate known facts from assumptions.
                        - Use one verdict: {reportLanguage.VerdictLabels}.
                        - The verdict must be the localized equivalent of this internal recommendation: {reportLanguage.MapVerdict(risks.RecommendedVerdict)}.
                        - Under the Verdict heading, always include: one localized verdict label, a short explanation, an At a glance table, and a Your questions answered subsection.
                        - The At a glance table must include these rows: Overall risk, Main concern, Technical risk, Listing transparency, Deal risk, Recommended next step.
                        - Use severity labels with icons in summary and risk tables: 🟢 Low, 🟠 Medium, 🔴 High, ⚪ Unknown. Localize the words but keep the icons.
                        - The Your questions answered subsection must use the structured userQuestions from the risk analysis.
                        - If userQuestions is empty, state that no explicit buyer questions were found and that the report focuses on listing risks, missing information, seller questions, and inspection points.
                        - Do not invent user questions. Mark unrelated questions as out of scope.
                        - Under Key Risks, include Top decision points with 3-5 numbered points and a compact Risk overview table.
                        - Technical Risks, Listing Risks, and Deal Risks must use markdown tables with columns: Risk, Severity, Evidence, Why it matters, How to verify.
                        - If a risk category has no meaningful risks, write one short cautious sentence instead of forcing a fake risk.
                        - Missing Information must use a markdown table with columns: Missing item, Why it matters, Priority.
                        - Questions for the Seller must group 6-10 copy-ready questions under relevant subheadings such as Price and payment, Vehicle history, Documents, Inspection, Logistics, Warranty, or Financing.
                        - Use grouped markdown checkboxes in Inspection Checklist under relevant subheadings such as Documents, Exterior, Interior, Test drive, Electronics, and Final handover.
                        - Vehicle Facts must use a clean markdown table.
                        - Include an Estimated Costs markdown table using PLN with columns: Cost item, Estimated amount, Notes, Priority / When to pay.
                        - Facts should use the {reportLanguage.EnglishName} equivalent of "Unknown" when unavailable.
                        - Do not mention internal model names, prompts, stages, or confidence machinery.
                        - If automatic crawler data was unavailable, do not expose technical crawler failure details.
                        - You may say that the report is based on the user-provided text/images when relevant.
                        - Risk tables should normally contain no more than 5 rows unless truly necessary.
                        - Never write "no risk", "zero risk", "this car is safe", "100% safe", "guaranteed accident-free", "seller is lying", or direct fraud accusations.
                        - Prefer cautious language such as "No major issue is visible from the available evidence, but this still requires verification during inspection."
                        - The Summary section must include 2-3 sentences, a clear recommended next action, and a reminder that the report is preliminary.

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

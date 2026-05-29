using AutoVerdict.Application.AI;
using AutoVerdict.Contracts.Reports;
using AutoVerdict.Infrastructure.AI;
using Microsoft.Extensions.Options;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class ReportRepairStage(
    AiStageRunner runner,
    IOptions<AiPipelineOptions> options)
{
    private const string StageName = "ReportRepair";
    private const string PromptVersion = "report-repair.v1";

    private readonly AiPipelineOptions _options = options.Value;

    public async Task<string> ExecuteAsync(
        Guid checkId,
        string markdown,
        ReportValidationResult validation,
        ReportLanguage reportLanguage,
        AiBudgetTracker budget,
        CancellationToken cancellationToken)
    {
        var stage = _options.GetStage(StageName, "claude-haiku-4-5", 8000);
        var response = await runner.RunAsync(
            new AiTextRequest(
                checkId,
                StageName,
                stage.Model,
                PromptVersion,
                BuildSystemPrompt(),
                [
                    new AiTextContent(
                        $"""
                        Repair this AutoVerdict markdown report so it passes validation.

                        CRITICAL LANGUAGE REQUIREMENT:
                        - The repaired report must be written in {reportLanguage.EnglishName} ({reportLanguage.NativeName}).
                        - Do not leave English prose, English section headings, or the English disclaimer unless the requested language is English.
                        - Preserve brand names, URLs, VINs, model names, and technical identifiers exactly.

                        Validation errors:
                        {string.Join("\n", validation.Errors.Select(e => "- " + e))}

                        Validation warnings:
                        {string.Join("\n", validation.Warnings.Select(w => "- " + w))}

                        Requirements:
                        - Preserve the original analysis content as much as possible.
                        - Do not add new unsupported facts.
                        - Add missing required sections if needed, using exactly these headings in this exact order:
                        {string.Join("\n", reportLanguage.RequiredHeadings.Select(h => "- " + h))}
                        - Use one localized verdict: {reportLanguage.VerdictLabels}.
                        - The report must end with this exact localized disclaimer after a horizontal rule:
                        ---
                        {reportLanguage.Disclaimer}
                        - Remove unsafe certainty language.
                        - Return only the repaired markdown report.

                        Report:
                        {markdown}
                        """),
                ],
                stage.MaxTokens),
            budget,
            validationWarningsJson: AiJson.Serialize(validation),
            cancellationToken: cancellationToken);

        return response.Text.Trim();
    }

    private static string BuildSystemPrompt() =>
        """
        You are AutoVerdict's report repair component.
        Fix markdown structure and unsafe wording only.
        Do not re-analyze the car from scratch.
        Return markdown only.
        """;
}

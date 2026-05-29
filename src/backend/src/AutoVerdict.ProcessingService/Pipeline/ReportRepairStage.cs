using AutoVerdict.Application.AI;
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

                        Validation errors:
                        {string.Join("\n", validation.Errors.Select(e => "- " + e))}

                        Validation warnings:
                        {string.Join("\n", validation.Warnings.Select(w => "- " + w))}

                        Requirements:
                        - Preserve the original analysis content as much as possible.
                        - Do not add new unsupported facts.
                        - Add missing required sections if needed.
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

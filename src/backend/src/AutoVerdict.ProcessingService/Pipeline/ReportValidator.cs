using System.Text.RegularExpressions;
using AutoVerdict.Application.AI;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed partial class ReportValidator
{
    private static readonly string[] RequiredHeadings =
    [
        "# Verdict",
        "# Key Risks",
        "## Technical Risks",
        "## Listing Risks",
        "## Deal Risks",
        "# Missing Information",
        "# Questions for the Seller",
        "# Inspection Checklist",
        "# Vehicle Facts",
        "# Estimated Costs",
        "# Summary",
    ];

    private static readonly string[] ForbiddenPhrases =
    [
        "this car is safe",
        "the seller is dishonest",
        "definitely damaged",
        "guaranteed",
        "no risk",
    ];

    public ReportValidationResult Validate(string markdown)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(markdown) || markdown.Length < 800)
            errors.Add("Report is empty or too short.");

        foreach (var heading in RequiredHeadings)
        {
            if (!markdown.Contains(heading, StringComparison.Ordinal))
                errors.Add($"Missing required heading: {heading}");
        }

        if (!markdown.Contains("AutoVerdict provides AI-assisted preliminary screening only", StringComparison.OrdinalIgnoreCase))
            errors.Add("Missing required disclaimer.");

        if (!AllowedVerdictRegex().IsMatch(markdown))
            errors.Add("Missing allowed verdict.");

        if (!markdown.Contains("|", StringComparison.Ordinal) ||
            !markdown.Contains("# Estimated Costs", StringComparison.Ordinal))
            warnings.Add("Estimated costs table may be missing.");

        if (!CheckboxRegex().IsMatch(markdown))
            warnings.Add("Inspection checklist does not appear to use markdown checkboxes.");

        foreach (var phrase in ForbiddenPhrases)
        {
            if (markdown.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                errors.Add($"Unsafe wording detected: {phrase}");
        }

        return new ReportValidationResult(errors.Count == 0, errors, warnings);
    }

    [GeneratedRegex(@"\b(Buy with caution|Buy|Avoid)\b", RegexOptions.IgnoreCase)]
    private static partial Regex AllowedVerdictRegex();

    [GeneratedRegex(@"- \[[ xX]\]")]
    private static partial Regex CheckboxRegex();
}

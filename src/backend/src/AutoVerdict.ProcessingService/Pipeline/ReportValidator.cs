using System.Text.RegularExpressions;
using AutoVerdict.Application.AI;
using AutoVerdict.Contracts.Reports;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed partial class ReportValidator
{
    public ReportValidationResult Validate(string markdown, ReportLanguage reportLanguage)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(markdown) || markdown.Length < 800)
            errors.Add("Report is empty or too short.");

        foreach (var heading in reportLanguage.RequiredHeadings)
        {
            if (!markdown.Contains(heading, StringComparison.Ordinal))
                errors.Add($"Missing required heading: {heading}");
        }

        if (!markdown.Contains(reportLanguage.Disclaimer, StringComparison.OrdinalIgnoreCase))
            errors.Add("Missing required disclaimer.");

        if (!ContainsAllowedVerdict(markdown, reportLanguage))
            errors.Add("Missing allowed verdict.");

        if (!markdown.Contains("|", StringComparison.Ordinal) ||
            !markdown.Contains(reportLanguage.RequiredHeadings[9], StringComparison.Ordinal))
            warnings.Add("Estimated costs table may be missing.");

        if (!CheckboxRegex().IsMatch(markdown))
            warnings.Add("Inspection checklist does not appear to use markdown checkboxes.");

        AddUnsafeLanguageFindings(markdown, errors, warnings);

        return new ReportValidationResult(errors.Count == 0, errors, warnings);
    }

    private static bool ContainsAllowedVerdict(string markdown, ReportLanguage reportLanguage) =>
        markdown.Contains(reportLanguage.BuyVerdict, StringComparison.OrdinalIgnoreCase) ||
        markdown.Contains(reportLanguage.CautionVerdict, StringComparison.OrdinalIgnoreCase) ||
        markdown.Contains(reportLanguage.AvoidVerdict, StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"- \[[ xX]\]")]
    private static partial Regex CheckboxRegex();

    private static void AddUnsafeLanguageFindings(
        string markdown,
        List<string> errors,
        List<string> warnings)
    {
        if (CarSafeRegex().IsMatch(markdown))
            errors.Add("Unsafe wording detected: this car is safe");
        if (DishonestSellerRegex().IsMatch(markdown))
            errors.Add("Unsafe wording detected: seller dishonesty claim");
        if (DefinitelyDamagedRegex().IsMatch(markdown))
            errors.Add("Unsafe wording detected: definite damage claim");
        if (NoRiskRegex().IsMatch(markdown))
            errors.Add("Unsafe wording detected: no risk claim");

        if (GuaranteedRiskRegex().IsMatch(markdown))
            warnings.Add("Potentially overconfident guarantee wording detected.");
    }

    [GeneratedRegex(@"\bthis car is safe\b", RegexOptions.IgnoreCase)]
    private static partial Regex CarSafeRegex();

    [GeneratedRegex(@"\b(the )?seller (is|seems|appears) dishonest\b", RegexOptions.IgnoreCase)]
    private static partial Regex DishonestSellerRegex();

    [GeneratedRegex(@"\bdefinitely damaged\b", RegexOptions.IgnoreCase)]
    private static partial Regex DefinitelyDamagedRegex();

    [GeneratedRegex(@"\b(no|zero) risk\b", RegexOptions.IgnoreCase)]
    private static partial Regex NoRiskRegex();

    [GeneratedRegex(@"\b(guaranteed safe|guaranteed accident-free|guaranteed risk-free|guaranteed clean)\b", RegexOptions.IgnoreCase)]
    private static partial Regex GuaranteedRiskRegex();
}

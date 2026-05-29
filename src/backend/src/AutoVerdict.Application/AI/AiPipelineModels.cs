using AutoVerdict.Contracts.Listing;

namespace AutoVerdict.Application.AI;

public sealed record EvidenceBundle(
    Guid CheckId,
    string UserDescriptionMarkdown,
    string? ListingUrl,
    string CrawlStatus,
    string? CrawlError,
    CarListingSnapshot? CrawledListing,
    IReadOnlyList<UserImageContent> UserImages,
    UserImageContent? ListingScreenshot);

public sealed record ExtractedVehicleFacts(
    string? Make,
    string? Model,
    int? Year,
    int? MileageKm,
    decimal? Price,
    string? Currency,
    string? FuelType,
    string? Transmission,
    string? Engine,
    string? SellerType,
    string? Location,
    string? Vin,
    bool VinPresent,
    bool ServiceHistoryMentioned,
    bool AccidentFreeClaimed,
    bool ImportedMentioned,
    bool FirstOwnerClaimed,
    IReadOnlyDictionary<string, object?> RawAttributes,
    IReadOnlyList<string> EvidenceNotes,
    IReadOnlyList<string> MissingFields,
    string Confidence);

public sealed record RiskAnalysisResult(
    string OverallRiskLevel,
    string RecommendedVerdict,
    string Confidence,
    IReadOnlyList<RiskItem> TechnicalRisks,
    IReadOnlyList<RiskItem> ListingRisks,
    IReadOnlyList<RiskItem> DealRisks,
    IReadOnlyList<string> MissingInformation,
    IReadOnlyList<string> SellerQuestions,
    IReadOnlyList<string> InspectionChecklist,
    IReadOnlyList<string> CostAssumptions,
    IReadOnlyList<string> Inconsistencies,
    bool NeedsEscalation,
    string? EscalationReason,
    string? MainConcern = null,
    string RecommendedNextStep = "",
    IReadOnlyList<UserQuestionAnswer>? UserQuestions = null);

public sealed record RiskItem(
    string Severity,
    string Title,
    string Explanation,
    string Source,
    string HowToVerify,
    string EvidenceStrength = "medium");

public sealed record UserQuestionAnswer(
    string Question,
    string Answer,
    string Status);

public sealed record FinalReportResult(
    string Markdown,
    string Verdict,
    string Confidence,
    bool WasRepaired,
    IReadOnlyList<string> ValidationWarnings);

public sealed record ReportValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

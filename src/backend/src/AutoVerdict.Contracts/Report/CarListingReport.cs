namespace AutoVerdict.Contracts.Report;

public sealed record VehicleReport(
    string CarSummary,
    ListingFacts ListingFacts,
    IReadOnlyList<string> ModelRisks,
    IReadOnlyList<string> ListingRisks,
    IReadOnlyList<string> DealRisks,
    EstimatedCosts EstimatedCosts,
    IReadOnlyList<string> SellerQuestions,
    IReadOnlyList<string> InspectionChecklist,
    string Recommendation,
    string Disclaimer);

public sealed record ListingFacts(
    string ListingUrl,
    string? Title,
    string? Make,
    string? Model,
    int? Year,
    int? MileageKm,
    decimal? Price,
    string? Currency,
    string? SellerType,
    string? Location);

public sealed record EstimatedCosts(
    decimal? PurchasePrice,
    decimal? RegistrationFee,
    decimal? InsuranceCost,
    decimal? PotentialRepairs,
    decimal? Total,
    string Currency,
    string Notes);

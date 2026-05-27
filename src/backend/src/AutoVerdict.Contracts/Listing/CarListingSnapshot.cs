namespace AutoVerdict.Contracts.Listing;

public sealed record CarListingSnapshot(
    string ListingUrl,
    string? Title,
    string? Make,
    string? Model,
    int? Year,
    int? MileageKm,
    decimal? Price,
    string? SellerName,
    string? SellerType,
    string? Location,
    string? Description,
    IReadOnlyDictionary<string, string> Attributes,
    string ScreenshotStorageKey,
    DateTimeOffset ExtractedAt);

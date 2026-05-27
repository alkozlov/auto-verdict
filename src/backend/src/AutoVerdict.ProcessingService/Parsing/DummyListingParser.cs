using AutoVerdict.Contracts.Listing;

namespace AutoVerdict.ProcessingService.Parsing;

public sealed class DummyListingParser(ILogger<DummyListingParser> logger) : ICarListingParser
{
    public Task<ListingParseResult> ParseAsync(
        Guid checkId,
        string listingUrl,
        string screenshotStorageKey,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "Check {CheckId}: no specialized parser available for URL {Url}; returning empty result.",
            checkId, listingUrl);

        var snapshot = new CarListingSnapshot(
            listingUrl,
            Title: null,
            Make: null,
            Model: null,
            Year: null,
            MileageKm: null,
            Price: null,
            SellerName: null,
            SellerType: null,
            Location: null,
            Description: null,
            Attributes: new Dictionary<string, string>(),
            screenshotStorageKey,
            DateTimeOffset.UtcNow);

        return Task.FromResult(new ListingParseResult(snapshot, [], "image/png"));
    }
}

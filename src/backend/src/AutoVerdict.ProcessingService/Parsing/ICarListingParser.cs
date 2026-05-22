namespace AutoVerdict.ProcessingService.Parsing;

public interface ICarListingParser
{
    Task<ListingParseResult> ParseAsync(
        Guid checkId,
        string listingUrl,
        string screenshotStorageKey,
        CancellationToken cancellationToken = default);
}

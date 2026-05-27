using AutoVerdict.Contracts.Listing;

namespace AutoVerdict.Application.AI;

public sealed record AiAnalysisRequest(
    Guid CheckId,
    string Description,
    string? ListingUrl = null,
    IReadOnlyList<UserImageContent>? UserImages = null,
    byte[]? ListingScreenshotBytes = null,
    string ListingScreenshotContentType = "image/png",
    CarListingSnapshot? CrawledListing = null);

public sealed record UserImageContent(byte[] Bytes, string ContentType);

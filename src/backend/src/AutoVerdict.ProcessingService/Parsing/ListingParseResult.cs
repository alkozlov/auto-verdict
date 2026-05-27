using AutoVerdict.Contracts.Listing;

namespace AutoVerdict.ProcessingService.Parsing;

public sealed record ListingParseResult(
    CarListingSnapshot Listing,
    byte[] ScreenshotBytes,
    string ScreenshotContentType,
    bool DetectedBlockOrCaptcha = false,
    string? CanonicalUrl = null,
    string? HtmlLanguage = null,
    string? CurrentUrl = null);

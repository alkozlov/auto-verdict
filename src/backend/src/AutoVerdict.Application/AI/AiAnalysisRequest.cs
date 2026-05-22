using AutoVerdict.Contracts.Listing;

namespace AutoVerdict.Application.AI;

public sealed record AiAnalysisRequest(
    Guid CheckId,
    CarListingSnapshot Listing,
    byte[] ScreenshotBytes,
    string ScreenshotContentType = "image/png");

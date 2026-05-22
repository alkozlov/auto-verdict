using AutoVerdict.Contracts.Listing;
using AutoVerdict.Contracts.Report;

namespace AutoVerdict.Application.AI;

public sealed record AiAnalysisResult(
    VehicleReport Report,
    CarListingSnapshot Listing,
    string ProviderName,
    string ModelName,
    long InputTokens,
    long OutputTokens);

using AutoVerdict.Contracts.Report;

namespace AutoVerdict.Application.AI;

public sealed record AiAnalysisResult(
    VehicleReport Report,
    string ProviderName,
    string ModelName,
    long InputTokens,
    long OutputTokens);

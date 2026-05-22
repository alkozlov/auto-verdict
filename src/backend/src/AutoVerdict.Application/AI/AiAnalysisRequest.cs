namespace AutoVerdict.Application.AI;

public sealed record AiAnalysisRequest(
    Guid CheckId,
    string VehicleIdentifier,
    string DocumentContent);

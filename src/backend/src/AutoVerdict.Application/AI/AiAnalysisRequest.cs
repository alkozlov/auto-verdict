namespace AutoVerdict.Application.AI;

public sealed record AiAnalysisRequest(
    Guid CheckId,
    string VehicleIdentifier,
    byte[] DocumentBytes,
    string ContentType = "application/octet-stream");

namespace AutoVerdict.Contracts.Report;

public sealed record VehicleReport(
    string VehicleIdentifier,
    string Verdict,
    OwnershipSection Ownership,
    MileageSection Mileage,
    AccidentSection Accidents,
    ServiceSection Service,
    LegalSection Legal);

public sealed record OwnershipSection(
    int OwnersCount,
    bool CommercialUseDetected,
    string? Notes);

public sealed record MileageSection(
    bool InconsistencyDetected,
    int? LastRecordedKm,
    string? Notes);

public sealed record AccidentSection(
    int TotalCount,
    bool SevereDamageDetected,
    string? Notes);

public sealed record ServiceSection(
    bool RegularMaintenanceConfirmed,
    DateTimeOffset? LastServiceDate,
    string? Notes);

public sealed record LegalSection(
    bool PledgeDetected,
    bool StolenDetected,
    bool WantedDetected,
    string? Notes);

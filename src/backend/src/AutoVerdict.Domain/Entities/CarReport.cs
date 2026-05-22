using AutoVerdict.Contracts.Report;

namespace AutoVerdict.Domain.Entities;

public sealed class CarReport
{
    public Guid Id { get; set; }
    public Guid CarCheckId { get; set; }
    public VehicleReport ReportData { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public CarCheck CarCheck { get; set; } = null!;
}

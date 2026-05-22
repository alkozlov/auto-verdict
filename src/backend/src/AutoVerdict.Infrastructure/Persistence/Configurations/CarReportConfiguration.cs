using System.Text.Json;
using AutoVerdict.Contracts.Report;
using AutoVerdict.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVerdict.Infrastructure.Persistence.Configurations;

public sealed class CarReportConfiguration : IEntityTypeConfiguration<CarReport>
{
    private static readonly JsonSerializerOptions JsonOpts = new();

    public void Configure(EntityTypeBuilder<CarReport> builder)
    {
        builder.ToTable("car_reports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReportData)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOpts),
                v => JsonSerializer.Deserialize<VehicleReport>(v, JsonOpts)!);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasIndex(r => r.CarCheckId)
            .IsUnique();

        builder.HasOne(r => r.CarCheck)
            .WithOne(c => c.Report)
            .HasForeignKey<CarReport>(r => r.CarCheckId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

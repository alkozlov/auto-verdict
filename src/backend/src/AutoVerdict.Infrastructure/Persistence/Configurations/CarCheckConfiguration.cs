using AutoVerdict.Contracts.Enums;
using AutoVerdict.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVerdict.Infrastructure.Persistence.Configurations;

public sealed class CarCheckConfiguration : IEntityTypeConfiguration<CarCheck>
{
    public void Configure(EntityTypeBuilder<CarCheck> builder)
    {
        builder.ToTable("car_checks");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.VehicleIdentifier)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.ListingUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(c => c.DocumentStorageKey)
            .HasMaxLength(500);

        builder.Property(c => c.Title)
            .HasMaxLength(500);

        builder.Property(c => c.Make)
            .HasMaxLength(100);

        builder.Property(c => c.Model)
            .HasMaxLength(100);

        builder.Property(c => c.Price)
            .HasPrecision(12, 2);

        builder.Property(c => c.Currency)
            .HasMaxLength(10);

        builder.Property(c => c.ScreenshotStorageKey)
            .HasMaxLength(500);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.FailureReason)
            .HasMaxLength(1000);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.ListingUrl);

        builder.HasOne(c => c.User)
            .WithMany(u => u.CarChecks)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

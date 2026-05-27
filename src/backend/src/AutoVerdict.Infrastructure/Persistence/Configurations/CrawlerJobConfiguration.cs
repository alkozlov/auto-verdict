using AutoVerdict.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVerdict.Infrastructure.Persistence.Configurations;

public sealed class CrawlerJobConfiguration : IEntityTypeConfiguration<CrawlerJob>
{
    public void Configure(EntityTypeBuilder<CrawlerJob> builder)
    {
        builder.ToTable("crawler_jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.ListingUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(j => j.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(j => j.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(j => j.RawData)
            .HasColumnType("jsonb");

        builder.Property(j => j.NormalizedData)
            .HasColumnType("jsonb");

        builder.Property(j => j.ScreenshotBucket)
            .HasMaxLength(200);

        builder.Property(j => j.ScreenshotObjectKey)
            .HasMaxLength(500);

        builder.Property(j => j.ScreenshotContentType)
            .HasMaxLength(100);

        builder.Property(j => j.ErrorCode)
            .HasMaxLength(100);

        builder.Property(j => j.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasIndex(j => j.ListingUrl).HasDatabaseName("IX_crawler_jobs_ListingUrl");
        builder.HasIndex(j => j.UserId).HasDatabaseName("IX_crawler_jobs_UserId");
        builder.HasIndex(j => j.Status).HasDatabaseName("IX_crawler_jobs_Status");
        builder.HasIndex(j => j.CreatedAt).HasDatabaseName("IX_crawler_jobs_CreatedAt");
    }
}

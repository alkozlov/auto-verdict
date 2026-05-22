using AutoVerdict.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVerdict.Infrastructure.Persistence.Configurations;

public sealed class UploadedFileConfiguration : IEntityTypeConfiguration<UploadedFile>
{
    public void Configure(EntityTypeBuilder<UploadedFile> builder)
    {
        builder.ToTable("uploaded_files");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.StorageKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.FileSizeBytes)
            .IsRequired();

        builder.Property(f => f.OriginalFileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        builder.HasIndex(f => f.UserId);

        builder.HasOne(f => f.User)
            .WithMany(u => u.UploadedFiles)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

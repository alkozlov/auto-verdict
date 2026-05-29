using AutoVerdict.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVerdict.Infrastructure.Persistence.Configurations;

public sealed class AiRunConfiguration : IEntityTypeConfiguration<AiRun>
{
    public void Configure(EntityTypeBuilder<AiRun> builder)
    {
        builder.ToTable("ai_runs");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).UseIdentityColumn();

        builder.Property(r => r.Stage)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Provider)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Model)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.PromptVersion)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.EstimatedCostEur)
            .HasPrecision(12, 6);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(r => r.EscalationReason)
            .HasMaxLength(1000);

        builder.Property(r => r.ValidationWarningsJson)
            .HasColumnType("jsonb");

        builder.HasIndex(r => r.CheckId);
        builder.HasIndex(r => new { r.CheckId, r.Stage });
        builder.HasIndex(r => r.CreatedAt);
    }
}

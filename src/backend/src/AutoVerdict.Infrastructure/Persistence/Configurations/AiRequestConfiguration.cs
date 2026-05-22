using AutoVerdict.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVerdict.Infrastructure.Persistence.Configurations;

public sealed class AiRequestConfiguration : IEntityTypeConfiguration<AiRequest>
{
    public void Configure(EntityTypeBuilder<AiRequest> builder)
    {
        builder.ToTable("ai_requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ProviderName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.ModelName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.InputTokens)
            .IsRequired();

        builder.Property(r => r.OutputTokens)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasIndex(r => r.CarCheckId);

        builder.HasOne(r => r.CarCheck)
            .WithMany(c => c.AiRequests)
            .HasForeignKey(r => r.CarCheckId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

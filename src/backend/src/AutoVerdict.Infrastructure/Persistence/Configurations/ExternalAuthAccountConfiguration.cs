using AutoVerdict.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVerdict.Infrastructure.Persistence.Configurations;

public sealed class ExternalAuthAccountConfiguration : IEntityTypeConfiguration<ExternalAuthAccount>
{
    public void Configure(EntityTypeBuilder<ExternalAuthAccount> builder)
    {
        builder.ToTable("external_auth_accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.ProviderUserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(a => new { a.Provider, a.ProviderUserId })
            .IsUnique();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.HasOne(a => a.User)
            .WithMany(u => u.ExternalAuthAccounts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

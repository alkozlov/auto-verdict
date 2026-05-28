using AutoVerdict.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVerdict.Infrastructure.Persistence.Configurations;

public sealed class PaymentOrderConfiguration : IEntityTypeConfiguration<PaymentOrder>
{
    public void Configure(EntityTypeBuilder<PaymentOrder> builder)
    {
        builder.ToTable("payment_orders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PackageKey).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ExternalOrderId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CreditsGranted).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.ExternalOrderId).IsUnique();
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using AutoVerdict.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVerdict.Infrastructure.Persistence.Configurations;

public sealed class CreditLedgerEntryConfiguration : IEntityTypeConfiguration<CreditLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CreditLedgerEntry> builder)
    {
        builder.ToTable("credit_ledger_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount)
            .IsRequired();

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ReferenceId);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.User)
            .WithMany(u => u.CreditLedger)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

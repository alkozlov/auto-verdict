using AutoVerdict.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVerdict.Infrastructure.Persistence.Configurations;

public sealed class UserCreditsConfiguration : IEntityTypeConfiguration<UserCredits>
{
    public void Configure(EntityTypeBuilder<UserCredits> builder)
    {
        builder.ToTable("user_credits",
            t => t.HasCheckConstraint("ck_user_credits_balance_non_negative", "\"Balance\" >= 0"));

        builder.HasKey(c => c.UserId);

        builder.Property(c => c.Balance)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.HasOne(c => c.User)
            .WithOne(u => u.Credits)
            .HasForeignKey<UserCredits>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

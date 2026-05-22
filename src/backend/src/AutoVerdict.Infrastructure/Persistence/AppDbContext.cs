using AutoVerdict.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoVerdict.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ExternalAuthAccount> ExternalAuthAccounts => Set<ExternalAuthAccount>();
    public DbSet<CreditLedgerEntry> CreditLedgerEntries => Set<CreditLedgerEntry>();
    public DbSet<UserCredits> UserCredits => Set<UserCredits>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

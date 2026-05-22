using Microsoft.EntityFrameworkCore;

namespace AutoVerdict.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}


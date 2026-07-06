using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure.Persistence;
using AutoVerdict.ProcessingService.Pipeline;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AutoVerdict.Api.Tests;

public sealed class AiSpendTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public AiSpendTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    private void AddRun(Guid checkId, decimal cost)
    {
        var now = DateTimeOffset.UtcNow;
        _db.AiRuns.Add(new AiRun
        {
            CheckId = checkId, Stage = "FactExtraction", Provider = "Claude",
            Model = "m", PromptVersion = "1", EstimatedCostEur = cost,
            Status = "Succeeded", StartedAt = now, CreatedAt = now,
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task SumsOnlyThisChecksRuns()
    {
        var checkId = Guid.NewGuid();
        AddRun(checkId, 0.30m);
        AddRun(checkId, 0.25m);
        AddRun(Guid.NewGuid(), 5.00m); // different check — excluded

        var sum = await AiSpend.SumPriorForCheckAsync(_db, checkId, CancellationToken.None);

        Assert.Equal(0.55m, sum, precision: 4);
    }

    [Fact]
    public async Task NoRuns_SumsToZero()
    {
        Assert.Equal(0m, await AiSpend.SumPriorForCheckAsync(_db, Guid.NewGuid(), CancellationToken.None));
    }
}

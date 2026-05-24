using System.Text.Json;
using AutoVerdict.Contracts.Messages;
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure.Persistence;

namespace AutoVerdict.ProcessingService.Crawler;

public sealed class CrawlerJobService(AppDbContext db)
{
    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Success", "Failed", "SkippedDuplicate", "InvalidUrl", "BlockedOrCaptcha", "UnsupportedSource"
    };

    public async Task<CrawlerJob?> FindAsync(Guid checkId, CancellationToken ct) =>
        await db.CrawlerJobs.FindAsync([checkId], ct);

    public static bool IsTerminal(CrawlerJob job) => TerminalStatuses.Contains(job.Status);

    public async Task<CrawlerJob> StartAsync(CarCheckRequestedMessage msg, string source, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var existing = await db.CrawlerJobs.FindAsync([msg.CheckId], ct);

        if (existing is not null)
        {
            existing.StartedAt = now;
            existing.Attempts++;
            existing.Status = "InProgress";
            existing.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
            return existing;
        }

        var job = new CrawlerJob
        {
            Id = msg.CheckId,
            UserId = msg.UserId,
            ListingUrl = msg.ListingUrl,
            Source = source,
            RequestedAt = msg.RequestedAt,
            StartedAt = now,
            Status = "InProgress",
            Attempts = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.CrawlerJobs.Add(job);
        await db.SaveChangesAsync(ct);
        return job;
    }

    public async Task CompleteAsync(Guid jobId, CrawlResult result, CancellationToken ct)
    {
        var job = await db.CrawlerJobs.FindAsync([jobId], ct)
            ?? throw new InvalidOperationException($"CrawlerJob {jobId} not found.");

        var now = DateTimeOffset.UtcNow;
        job.Status = result.Status;
        job.FinishedAt = now;
        job.UpdatedAt = now;
        job.RawData = result.RawData.Count > 0 ? JsonSerializer.Serialize(result.RawData) : null;
        job.NormalizedData = result.NormalizedData.Count > 0 ? JsonSerializer.Serialize(result.NormalizedData) : null;
        job.ErrorCode = result.ErrorCode;
        job.ErrorMessage = result.ErrorMessage;
        job.IsRetryable = result.IsRetryable;
        job.ScreenshotBucket = result.ScreenshotBucket;
        job.ScreenshotObjectKey = result.ScreenshotObjectKey;
        job.ScreenshotContentType = result.ScreenshotContentType;
        job.ScreenshotSizeBytes = result.ScreenshotSizeBytes;

        await db.SaveChangesAsync(ct);
    }
}

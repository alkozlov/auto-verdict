namespace AutoVerdict.Domain.Entities;

public sealed class CrawlerJob
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ListingUrl { get; set; } = null!;
    public string Source { get; set; } = null!;
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string Status { get; set; } = null!;
    public int Attempts { get; set; }
    public string? RawData { get; set; }
    public string? NormalizedData { get; set; }
    public string? ScreenshotBucket { get; set; }
    public string? ScreenshotObjectKey { get; set; }
    public string? ScreenshotContentType { get; set; }
    public long? ScreenshotSizeBytes { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool? IsRetryable { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

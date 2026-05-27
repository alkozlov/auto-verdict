namespace AutoVerdict.Contracts.Messages;

public sealed record CarCheckCrawledMessage(
    Guid CheckId,
    Guid UserId,
    string ListingUrl,
    DateTimeOffset RequestedAt,
    DateTimeOffset CrawledAt,
    string Source,
    string Status,
    Dictionary<string, object?> RawData,
    Dictionary<string, object?> NormalizedData,
    ScreenshotInfo? Screenshot,
    CrawlerError? Error);

public sealed record ScreenshotInfo(
    string Bucket,
    string ObjectKey,
    string ContentType,
    long SizeBytes,
    string? PublicUrl);

public sealed record CrawlerError(
    string Code,
    string Message,
    bool IsRetryable);

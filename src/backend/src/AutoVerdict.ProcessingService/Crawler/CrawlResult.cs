using AutoVerdict.ProcessingService.Parsing;

namespace AutoVerdict.ProcessingService.Crawler;

public sealed record CrawlResult(
    string Status,
    string? Source,
    string? ErrorCode,
    string? ErrorMessage,
    bool? IsRetryable,
    string? ScreenshotBucket,
    string? ScreenshotObjectKey,
    string? ScreenshotContentType,
    long? ScreenshotSizeBytes,
    Dictionary<string, object?> RawData,
    Dictionary<string, object?> NormalizedData,
    ListingParseResult? ParseResult)
{
    public bool IsSuccess => Status == "Success";
    public bool IsRetryableError => IsRetryable == true;

    public static CrawlResult Success(
        ListingParseResult parsed,
        string source,
        string bucket,
        string objectKey,
        string contentType,
        long sizeBytes,
        Dictionary<string, object?> rawData,
        Dictionary<string, object?> normalizedData) =>
        new(
            Status: "Success",
            Source: source,
            ErrorCode: null,
            ErrorMessage: null,
            IsRetryable: null,
            ScreenshotBucket: bucket,
            ScreenshotObjectKey: objectKey,
            ScreenshotContentType: contentType,
            ScreenshotSizeBytes: sizeBytes,
            RawData: rawData,
            NormalizedData: normalizedData,
            ParseResult: parsed);

    public static CrawlResult Failure(string status, string errorCode, string message, bool retryable) =>
        new(
            Status: status,
            Source: null,
            ErrorCode: errorCode,
            ErrorMessage: message,
            IsRetryable: retryable,
            ScreenshotBucket: null,
            ScreenshotObjectKey: null,
            ScreenshotContentType: null,
            ScreenshotSizeBytes: null,
            RawData: [],
            NormalizedData: [],
            ParseResult: null);
}

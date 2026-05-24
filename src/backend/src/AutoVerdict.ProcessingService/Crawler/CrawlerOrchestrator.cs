using AutoVerdict.Application.Storage;
using AutoVerdict.Contracts.Messages;
using AutoVerdict.Infrastructure.Storage;
using AutoVerdict.ProcessingService.Parsing;
using Microsoft.Extensions.Options;

namespace AutoVerdict.ProcessingService.Crawler;

public sealed class CrawlerOrchestrator(
    ICarListingParser parser,
    IDocumentStorageClient storage,
    CrawlerJobService jobService,
    DomainRateLimiter rateLimiter,
    IOptions<CrawlerOptions> crawlerOptions,
    IOptions<S3Options> s3Options,
    ILogger<CrawlerOrchestrator> logger)
{
    public async Task<CrawlResult> CrawlAsync(
        CarCheckRequestedMessage message,
        CancellationToken cancellationToken)
    {
        // 1. Validate URL + detect source
        if (!TryDetectSource(message.ListingUrl, out var source, out var uri))
        {
            logger.LogWarning(
                "Check {CheckId}: invalid or unsupported URL {Url}.",
                message.CheckId, message.ListingUrl);

            return CrawlResult.Failure(
                "InvalidUrl", "INVALID_URL",
                $"URL '{message.ListingUrl}' is not a supported public listing URL.",
                retryable: false);
        }

        // 2. DB idempotency check
        var existing = await jobService.FindAsync(message.CheckId, cancellationToken);
        if (existing is not null && CrawlerJobService.IsTerminal(existing))
        {
            logger.LogInformation(
                "Check {CheckId}: already processed with status {Status}. Skipping.",
                message.CheckId, existing.Status);

            return CrawlResult.Failure(
                "SkippedDuplicate", "DUPLICATE_CHECK_ID",
                $"CheckId {message.CheckId} was already processed with status {existing.Status}.",
                retryable: false);
        }

        // 3. Create/update crawler job record
        var job = await jobService.StartAsync(message, source!, cancellationToken);

        var domain = uri!.Host;
        var rateLimitAcquired = false;

        try
        {
            // 4. Apply rate limiting
            await rateLimiter.WaitAsync(domain, cancellationToken);
            rateLimitAcquired = true;

            // 5. Build screenshot storage key following spec pattern
            var now = DateTimeOffset.UtcNow;
            var screenshotKey = $"car-checks/{now:yyyy}/{now:MM}/{now:dd}/{message.CheckId}/otomoto-page.png";

            // 6. Parse the page
            logger.LogInformation(
                "Check {CheckId}: crawling {Url} (source={Source}, attempt={Attempt}).",
                message.CheckId, message.ListingUrl, source, job.Attempts);

            ListingParseResult parsed;
            try
            {
                parsed = await parser.ParseAsync(
                    message.CheckId, message.ListingUrl, screenshotKey, cancellationToken);
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning(ex, "Check {CheckId}: navigation timed out.", message.CheckId);
                var timeoutResult = CrawlResult.Failure("Timeout", "NAVIGATION_TIMEOUT", ex.Message, retryable: true);
                await jobService.CompleteAsync(job.Id, timeoutResult, cancellationToken);
                return timeoutResult;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Check {CheckId}: page crawl failed.", message.CheckId);
                var fetchResult = CrawlResult.Failure("FetchFailed", "FETCH_FAILED", ex.Message, retryable: true);
                await jobService.CompleteAsync(job.Id, fetchResult, cancellationToken);
                return fetchResult;
            }

            // 7. Block / CAPTCHA detection
            if (parsed.DetectedBlockOrCaptcha)
            {
                var captchaRetryable = GetSourceOptions(source!) is { CaptchaIsRetryable: true };
                logger.LogWarning(
                    "Check {CheckId}: block/CAPTCHA detected at {Url}.", message.CheckId, message.ListingUrl);
                var blockedResult = CrawlResult.Failure(
                    "BlockedOrCaptcha", "BLOCKED_OR_CAPTCHA",
                    "Page appears to be blocked or showing a CAPTCHA.",
                    retryable: captchaRetryable);
                await jobService.CompleteAsync(job.Id, blockedResult, cancellationToken);
                return blockedResult;
            }

            // 8. Upload screenshot
            long screenshotBytes;
            try
            {
                screenshotBytes = parsed.ScreenshotBytes.Length;
                await using var stream = new MemoryStream(parsed.ScreenshotBytes);
                await storage.UploadAsync(screenshotKey, stream, parsed.ScreenshotContentType, cancellationToken);

                logger.LogInformation(
                    "Check {CheckId}: screenshot uploaded to {Key} ({Bytes} bytes).",
                    message.CheckId, screenshotKey, screenshotBytes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Check {CheckId}: screenshot upload failed.", message.CheckId);
                var uploadResult = CrawlResult.Failure(
                    "StorageUploadFailed", "STORAGE_UPLOAD_FAILED", ex.Message, retryable: true);
                await jobService.CompleteAsync(job.Id, uploadResult, cancellationToken);
                return uploadResult;
            }

            // 9. Build minimal data payloads
            var rawData = new Dictionary<string, object?>
            {
                ["page_title"] = parsed.Listing.Title,
                ["canonical_url"] = parsed.CanonicalUrl,
                ["current_url"] = parsed.CurrentUrl,
                ["source"] = source,
                ["html_language"] = parsed.HtmlLanguage,
                ["meta_description"] = parsed.Listing.Description,
                ["detected_block_or_captcha"] = parsed.DetectedBlockOrCaptcha,
            };

            var normalizedData = new Dictionary<string, object?>
            {
                ["source"] = source,
                ["url"] = parsed.CurrentUrl ?? message.ListingUrl,
                ["title"] = parsed.Listing.Title,
                ["is_publicly_accessible"] = true,
                ["detected_block_or_captcha"] = false,
            };

            var bucket = s3Options.Value.Bucket;

            // 10. Persist and return
            var successResult = CrawlResult.Success(
                parsed, source!, bucket, screenshotKey,
                parsed.ScreenshotContentType, screenshotBytes,
                rawData, normalizedData);

            await jobService.CompleteAsync(job.Id, successResult, cancellationToken);

            logger.LogInformation(
                "Check {CheckId}: crawl succeeded (source={Source}).", message.CheckId, source);

            return successResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Check {CheckId}: unexpected crawl failure.", message.CheckId);
            var failResult = CrawlResult.Failure("Failed", "UNKNOWN_ERROR", ex.Message, retryable: true);
            try { await jobService.CompleteAsync(job.Id, failResult, cancellationToken); } catch { }
            return failResult;
        }
        finally
        {
            if (rateLimitAcquired)
                rateLimiter.Release(domain);
        }
    }

    private bool TryDetectSource(string rawUrl, out string? source, out Uri? uri)
    {
        source = null;
        uri = null;

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out uri))
            return false;

        if (uri.Scheme is not "http" and not "https")
            return false;

        var host = uri.Host;
        var path = uri.AbsolutePath;

        foreach (var (name, opts) in crawlerOptions.Value.Sources)
        {
            if (!opts.AllowedDomains.Any(d =>
                host.Equals(d, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (opts.BlockedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                return false;

            source = name.ToLowerInvariant();
            return true;
        }

        return false;
    }

    private SourceCrawlerOptions? GetSourceOptions(string source) =>
        crawlerOptions.Value.Sources.TryGetValue(source, out var opts) ? opts : null;
}

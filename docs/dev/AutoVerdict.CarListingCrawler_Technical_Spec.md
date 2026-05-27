# Technical Specification: `AutoVerdict.CarListingCrawler`

## 1. Service Goal

Implement a separate microservice named `AutoVerdict.CarListingCrawler`.

The service receives car listing crawl requests through **NATS JetStream**, opens a public car listing page using Playwright, extracts basic page-level data, captures a screenshot of the listing page, uploads the screenshot to the existing file storage based on **SeaweedFS**, normalizes the result, and publishes the processing result back to **NATS JetStream**.

The service must not perform authentication, CAPTCHA bypassing, anti-bot bypassing, Google login automation, social login automation, or any action intended to access private or account-only data.

The service should only process publicly accessible listing pages.

---

## 2. Input Message

The service should subscribe to a NATS JetStream subject, for example:

```text
car-check.requested
```

Input payload:

```csharp
public sealed record CarCheckRequestedMessage(
    Guid CheckId,
    Guid UserId,
    string ListingUrl,
    DateTimeOffset RequestedAt);
```

**Important: the C# code above is only an example. The implementation agent may make small adjustments if needed to fit the existing project architecture and conventions.**

Requirements:

```text
- CheckId must be used as the idempotency key.
- ListingUrl must be validated before opening it in the browser.
- Initially, only public otomoto.pl listing URLs need to be supported.
- The architecture should allow adding other sources later, for example mobile.de, autoscout24, allegro, olx, etc.
```

---

## 3. Output Message

After processing, the service should publish the result to a NATS JetStream subject, for example:

```text
car-check.crawled
```

Recommended output payload:

```csharp
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
```

**Important: the C# code above is only an example. The implementation agent may make small adjustments if needed to fit the existing project architecture and conventions.**

Possible `Status` values:

```text
Success
SkippedDuplicate
InvalidUrl
FetchFailed
ParseFailed
ScreenshotFailed
StorageUploadFailed
BlockedOrCaptcha
Timeout
Failed
```

A single result subject is preferred:

```text
car-check.crawled
```

The status should be part of the payload. This keeps downstream processing simpler than maintaining separate success and failure subjects.

---

## 4. PostgreSQL Persistence Recommendation

Recommended approach: persist a minimal technical result in PostgreSQL, but do not use PostgreSQL as the primary result delivery mechanism.

NATS should remain the event transport mechanism.

PostgreSQL should be used for:

```text
- idempotency;
- audit trail;
- retry tracking;
- debugging;
- processing history;
- storing screenshot references;
- safe result re-publishing if needed.
```

In other words:

```text
NATS JetStream = event transport and queue.
PostgreSQL = crawler job state, idempotency, diagnostics, and history.
SeaweedFS = screenshot storage.
```

Downstream services should consume the result from NATS, not poll PostgreSQL as the main integration mechanism.

---

## 5. Recommended Database Structures

The implementation agent should adapt the database schema to the existing project architecture, naming conventions, migrations approach, and infrastructure setup.

The following structures are recommended.

### 5.1 `crawler_jobs`

```sql
CREATE TABLE crawler_jobs (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    listing_url TEXT NOT NULL,
    source TEXT NOT NULL,
    requested_at TIMESTAMPTZ NOT NULL,
    started_at TIMESTAMPTZ,
    finished_at TIMESTAMPTZ,
    status TEXT NOT NULL,
    attempts INT NOT NULL DEFAULT 0,
    raw_data JSONB,
    normalized_data JSONB,
    screenshot_bucket TEXT,
    screenshot_object_key TEXT,
    screenshot_content_type TEXT,
    screenshot_size_bytes BIGINT,
    error_code TEXT,
    error_message TEXT,
    is_retryable BOOLEAN,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_crawler_jobs_listing_url ON crawler_jobs(listing_url);
CREATE INDEX ix_crawler_jobs_user_id ON crawler_jobs(user_id);
CREATE INDEX ix_crawler_jobs_status ON crawler_jobs(status);
CREATE INDEX ix_crawler_jobs_created_at ON crawler_jobs(created_at);
```

**Important: the SQL above is only an example. The implementation agent may make small adjustments if needed to fit the existing project architecture and conventions.**

### 5.2 `crawled_listing_snapshots`

Recommended optional table for URL-based deduplication and historical listing snapshots:

```sql
CREATE TABLE crawled_listing_snapshots (
    id UUID PRIMARY KEY,
    source TEXT NOT NULL,
    listing_url TEXT NOT NULL,
    url_hash TEXT NOT NULL,
    content_hash TEXT,
    normalized_data JSONB,
    screenshot_object_key TEXT,
    crawled_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ux_crawled_listing_snapshots_source_url_hash
ON crawled_listing_snapshots(source, url_hash);
```

**Important: the SQL above is only an example. The implementation agent may make small adjustments if needed to fit the existing project architecture and conventions.**

Required deduplication:

```text
- CheckId deduplication is mandatory.
- ListingUrl deduplication should be configurable and TTL-based.
```

---

## 6. Architecture Recommendations

The existing project architecture is not known in this specification. Therefore, the implementation agent must not blindly create an isolated architecture that conflicts with the current solution structure.

The service must be named:

```text
AutoVerdict.CarListingCrawler
```

The implementation agent should integrate the service into the existing architecture and follow existing project conventions for:

```text
- project layout;
- dependency injection;
- configuration;
- logging;
- persistence;
- migrations;
- messaging abstractions;
- SeaweedFS integration;
- Docker and deployment conventions;
- health checks;
- observability.
```

Recommended internal responsibilities/modules:

```text
- NATS JetStream consumer for crawl requests;
- NATS JetStream publisher for crawl results;
- crawl orchestration layer;
- source detection;
- OTOMOTO source handler;
- browser lifecycle management;
- screenshot service;
- SeaweedFS file storage adapter or reuse of existing storage abstraction;
- normalization layer;
- per-domain rate limiter;
- deduplication service;
- persistence layer for crawler job state.
```

Recommended interfaces, if they fit the existing architecture:

```csharp
public interface IListingSourceHandler
{
    string Source { get; }

    bool CanHandle(Uri uri);

    Task<CrawlExtractResult> ExtractAsync(
        IPage page,
        Uri listingUri,
        CancellationToken cancellationToken);
}

public sealed record CrawlExtractResult(
    Dictionary<string, object?> RawData,
    Dictionary<string, object?> Metadata);
```

**Important: the C# code above is only an example. The implementation agent may make small adjustments if needed to fit the existing project architecture and conventions.**

```csharp
public interface IListingNormalizer
{
    Dictionary<string, object?> Normalize(
        string source,
        Dictionary<string, object?> rawData);
}
```

**Important: the C# code above is only an example. The implementation agent may make small adjustments if needed to fit the existing project architecture and conventions.**

```csharp
public interface IFileStorage
{
    Task<StoredFileInfo> UploadAsync(
        Stream content,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken);
}

public sealed record StoredFileInfo(
    string Bucket,
    string ObjectKey,
    string ContentType,
    long SizeBytes,
    string? PublicUrl);
```

**Important: the C# code above is only an example. The implementation agent may make small adjustments if needed to fit the existing project architecture and conventions.**

If the project already contains similar abstractions, reuse them instead of creating duplicates.

---

## 7. Processing Flow

Processing one message should follow this flow:

```text
1. Receive a message from NATS JetStream.
2. Validate the payload.
3. Check idempotency by CheckId.
4. Detect the listing source from ListingUrl.
5. Verify that the source is supported.
6. Verify that the URL is public and does not point to login, account, auth, API, AJAX, or contact endpoints.
7. Create or update a crawler job record with InProgress status.
8. Apply domain-level rate limiting before opening the page.
9. Open the page with Playwright.
10. Wait for the page to reach a reasonable loading state.
11. Extract minimal page-level data.
12. Capture a screenshot of the page.
13. Upload the screenshot to SeaweedFS.
14. Normalize extracted data.
15. Save the processing result to PostgreSQL.
16. Publish the result message to NATS JetStream.
17. Acknowledge the original message only after the processing result is safely handled.
```

If a retryable error occurs:

```text
- save attempt and error details in PostgreSQL;
- use JetStream retry/backoff policy or controlled internal retry;
- do not publish a Success result;
- after max attempts are exceeded, publish a Failed result.
```

If a non-retryable error occurs:

```text
- save failed status;
- publish a result event with the failed status;
- acknowledge the original message.
```

---

## 8. Playwright Browser Strategy for Ubuntu Server 24.04 on VPS

The project will run on a VPS with Ubuntu Server 24.04. The crawler should therefore use a conservative, resource-aware Playwright setup.

Recommended strategy:

```text
- Use headless Chromium.
- Do not launch a new browser process for every URL.
- Keep one Browser instance per service process.
- Create a new BrowserContext for each crawl job.
- Create a new Page for each crawl job.
- Close the Page and BrowserContext after each job.
- Restart the Browser instance periodically, for example after N processed jobs or after a configured lifetime.
```

Rationale:

```text
- launching Chromium for every URL is expensive;
- reusing the same Page is unsafe because state may leak between jobs;
- a separate BrowserContext gives isolation for cookies, cache, and localStorage;
- browser history and authentication cookies should not be persisted;
- the crawler must not depend on bypassing anti-bot systems.
```

Recommended launch options:

```csharp
new BrowserTypeLaunchOptions
{
    Headless = true,
    Args =
    [
        "--disable-dev-shm-usage",
        "--no-sandbox",
        "--disable-setuid-sandbox",
        "--disable-gpu"
    ]
}
```

**Important: the C# code above is only an example. The implementation agent may make small adjustments if needed to fit the existing project architecture, Playwright version, Docker setup, and security requirements.**

Recommended context options:

```csharp
new BrowserNewContextOptions
{
    ViewportSize = new ViewportSize
    {
        Width = 1920,
        Height = 1080
    },
    Locale = "pl-PL",
    TimezoneId = "Europe/Warsaw",
    UserAgent = configuredUserAgent
}
```

**Important: the C# code above is only an example. The implementation agent may make small adjustments if needed to fit the existing project architecture, Playwright version, Docker setup, and security requirements.**

Notes:

```text
- --no-sandbox may be acceptable in Docker/VPS environments, but running the container as a non-root user and keeping sandboxing enabled is preferable if the environment supports it.
- Browser history should not be stored.
- Cookies should not be reused between crawl jobs.
- Authentication state must not be persisted.
```

The implementation must not use:

```text
- login automation;
- Google OAuth automation;
- saved Google cookies;
- stealth plugins;
- CAPTCHA solving;
- fingerprint spoofing;
- proxy rotation intended to bypass restrictions;
- aggressive crawling.
```

---

## 9. Screenshot Requirements

Primary screenshot strategy:

```csharp
await page.ScreenshotAsync(new PageScreenshotOptions
{
    Path = path,
    FullPage = true,
    Type = ScreenshotType.Png
});
```

**Important: the C# code above is only an example. The implementation agent may make small adjustments if needed to fit the existing project architecture, Playwright version, and storage flow.**

If full-page screenshot is not possible or the page is too large:

```text
1. Scroll to the top of the page.
2. Set a large viewport, for example 1920x3000 or 1920x4000.
3. Capture the visible viewport.
4. Mark the screenshot as partial in metadata.
```

Recommended storage object key:

```text
car-checks/{yyyy}/{MM}/{dd}/{checkId}/otomoto-page.png
```

Example:

```text
car-checks/2026/05/24/7f4c.../otomoto-page.png
```

Content type:

```text
image/png
```

---

## 10. Extraction Layer

Detailed extraction rules are intentionally out of scope for this task. They will be provided later.

For the first implementation, the extraction layer should support only minimal page-level extraction.

Extract at least:

```text
- page_title;
- canonical_url, if present;
- current_url;
- source;
- html_language, if present;
- meta_description, if present;
- detected_block_or_captcha: boolean.
```

Do not extract:

```text
- VIN if it is not publicly visible;
- seller phone hidden behind an action;
- account-only data;
- private fields;
- data requiring login.
```

The extraction layer should be source-specific and extensible so that detailed OTOMOTO parsing can be added later without rewriting the crawler orchestration.

---

## 11. Normalization Layer

Add a normalization layer even if the first version is minimal.

Initial normalized data may contain:

```json
{
  "source": "otomoto",
  "url": "...",
  "title": "...",
  "is_publicly_accessible": true,
  "detected_block_or_captcha": false
}
```

Later, the normalized data model will include fields such as:

```text
- brand;
- model;
- generation;
- year;
- mileage_km;
- fuel_type;
- engine_capacity_cm3;
- engine_power_hp;
- transmission;
- drivetrain;
- body_type;
- color;
- price;
- currency;
- location;
- seller_type;
- description;
- images.
```

---

## 12. Rate Limiting

Implement rate limiting at the domain/source level.

Initial recommended limits for `otomoto.pl`:

```text
- max concurrency: 1;
- delay between requests: 10-30 seconds with jitter;
- max requests per minute: 3;
- max retries per job: 3;
- exponential backoff on retryable failures.
```

Rate limiting must be applied before opening the page in Playwright.

Recommended configuration shape:

```json
{
  "Crawler": {
    "DefaultTimeoutSeconds": 60,
    "MaxAttempts": 3,
    "BrowserRestartAfterJobs": 100,
    "Sources": {
      "otomoto": {
        "AllowedDomains": [ "otomoto.pl", "www.otomoto.pl" ],
        "MaxConcurrency": 1,
        "MinDelaySeconds": 10,
        "MaxDelaySeconds": 30,
        "MaxRequestsPerMinute": 3
      }
    }
  }
}
```

**Important: the JSON above is only an example. The implementation agent may make small adjustments if needed to fit the existing project configuration style and conventions.**

---

## 13. Crawler Queue

NATS JetStream is the queue layer.

Requirements:

```text
- use a durable consumer;
- use manual acknowledgement;
- control max in-flight messages through configuration;
- use retry/backoff through JetStream policy or controlled internal retry;
- support graceful shutdown;
- do not acknowledge the input message before processing is safely completed;
- avoid infinite reprocessing of permanently failing messages.
```

Recommended subjects:

```text
car-check.requested
car-check.crawled
```

A separate failure subject is optional. The preferred approach is to publish all results to `car-check.crawled` and use the `Status` field inside the payload.

---

## 14. Deduplication

Implement two levels of deduplication.

### 14.1 CheckId Deduplication

Mandatory.

```text
If CheckId already exists in crawler job storage with a terminal status, do not process it again.
```

Terminal statuses include, at minimum:

```text
Success
Failed
SkippedDuplicate
InvalidUrl
BlockedOrCaptcha
```

### 14.2 ListingUrl Deduplication

Configurable.

```text
If the same URL was successfully processed recently, for example within the last 24 hours, the service may skip reopening the page and return the existing result.
```

Recommended configuration:

```json
{
  "Deduplication": {
    "EnableUrlDeduplication": true,
    "UrlDeduplicationTtlHours": 24
  }
}
```

**Important: the JSON above is only an example. The implementation agent may make small adjustments if needed to fit the existing project configuration style and conventions.**

For this pet project, URL deduplication should be enabled, but it should be TTL-based rather than permanent because listing price, status, and content can change.

---

## 15. Block and CAPTCHA Detection

The service should detect blocked pages or CAPTCHA/human verification pages, but must not attempt to bypass them.

Detection signals may include:

```text
- text similar to "nie jesteś robotem";
- CAPTCHA widget or human verification widget;
- URL containing authentication, login, captcha, or similar paths;
- HTTP 403 or 429 responses;
- page content clearly asking for human verification.
```

Recommended behavior:

```text
Status = BlockedOrCaptcha
Error.Code = "BLOCKED_OR_CAPTCHA"
Error.Message = short diagnostic message
Error.IsRetryable = configurable, but false by default for OTOMOTO
```

For OTOMOTO, `BlockedOrCaptcha` should be treated as non-retryable by default to prevent the service from repeatedly hitting the site.

---

## 16. URL Validation

Allow only public listing URLs.

For OTOMOTO, initially allow hosts:

```text
otomoto.pl
www.otomoto.pl
```

Reject URLs containing paths such as:

```text
/authentication
/account
/myaccount
/login
/oauth
/api
/ajax
/contact
```

Reject suspicious or unsupported URLs before opening them in Playwright.

---

## 17. SeaweedFS Upload

Use the existing file storage integration if the project already has one.

If a new abstraction is required, the following shape is recommended:

```csharp
public interface IFileStorage
{
    Task<StoredFileInfo> UploadAsync(
        Stream content,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken);
}

public sealed record StoredFileInfo(
    string Bucket,
    string ObjectKey,
    string ContentType,
    long SizeBytes,
    string? PublicUrl);
```

**Important: the C# code above is only an example. The implementation agent may make small adjustments if needed to fit the existing project architecture and existing SeaweedFS integration.**

Recommended configuration shape:

```json
{
  "SeaweedFs": {
    "Endpoint": "http://seaweedfs:8333",
    "Bucket": "car-checks",
    "AccessKey": "",
    "SecretKey": "",
    "UseS3Api": true
  }
}
```

**Important: the JSON above is only an example. The implementation agent may make small adjustments if needed to fit the existing project configuration style and existing SeaweedFS setup.**

Prefer the S3-compatible API of SeaweedFS if it is already enabled in the project.

---

## 18. Docker and Ubuntu Server 24.04 Runtime Notes

The service must be runnable in Docker on Ubuntu Server 24.04.

The implementation should ensure that Playwright browser dependencies are installed in the runtime image or handled through an appropriate base image.

The Docker setup should account for:

```text
- headless Chromium dependencies;
- /dev/shm limitations;
- non-root container user if possible;
- health checks;
- environment-based configuration;
- connection to NATS, PostgreSQL, and SeaweedFS.
```

If needed, Chromium launch args may include:

```text
--disable-dev-shm-usage
```

Use `--no-sandbox` only if required by the deployment environment. Prefer a more secure container setup where possible.

The agent should adapt Docker and deployment configuration to the existing project structure.

---

## 19. Observability

Add structured logging with fields such as:

```text
- check_id;
- user_id;
- listing_url;
- source;
- status;
- attempt;
- duration_ms;
- screenshot_object_key;
- error_code.
```

Recommended metrics:

```text
crawler_jobs_total
crawler_jobs_success_total
crawler_jobs_failed_total
crawler_jobs_blocked_total
crawler_duration_ms
crawler_screenshot_duration_ms
crawler_storage_upload_duration_ms
crawler_active_browser_contexts
```

Add health checks if the existing architecture supports them:

```text
/health/live
/health/ready
```

Readiness should verify, where practical:

```text
- PostgreSQL connectivity;
- NATS connectivity;
- SeaweedFS connectivity;
- Playwright browser can start or is already available.
```

---

## 20. Recommended Configuration

The following configuration is recommended as a reference only. The implementation agent should adapt it to the existing configuration style.

```json
{
  "Nats": {
    "Url": "nats://localhost:4222",
    "Stream": "CAR_CHECKS",
    "RequestSubject": "car-check.requested",
    "ResultSubject": "car-check.crawled",
    "DurableConsumer": "car-listing-crawler",
    "MaxAckPending": 10
  },
  "Crawler": {
    "DefaultTimeoutSeconds": 60,
    "MaxAttempts": 3,
    "BrowserRestartAfterJobs": 100,
    "NavigationWaitUntil": "domcontentloaded",
    "Sources": {
      "otomoto": {
        "AllowedDomains": [ "otomoto.pl", "www.otomoto.pl" ],
        "MaxConcurrency": 1,
        "MinDelaySeconds": 10,
        "MaxDelaySeconds": 30,
        "MaxRequestsPerMinute": 3
      }
    }
  },
  "Playwright": {
    "Browser": "chromium",
    "Headless": true,
    "ViewportWidth": 1920,
    "ViewportHeight": 1080,
    "Locale": "pl-PL",
    "TimezoneId": "Europe/Warsaw"
  },
  "Deduplication": {
    "EnableUrlDeduplication": true,
    "UrlDeduplicationTtlHours": 24
  },
  "SeaweedFs": {
    "Endpoint": "http://localhost:8333",
    "Bucket": "car-checks",
    "UseS3Api": true
  }
}
```

**Important: the JSON above is only an example. The implementation agent may make small adjustments if needed to fit the existing project architecture, configuration conventions, and infrastructure setup.**

---

## 21. Error Handling

Recommended error codes:

```text
INVALID_URL
UNSUPPORTED_SOURCE
DUPLICATE_CHECK_ID
DUPLICATE_LISTING_URL
NAVIGATION_TIMEOUT
FETCH_FAILED
PARSE_FAILED
SCREENSHOT_FAILED
STORAGE_UPLOAD_FAILED
BLOCKED_OR_CAPTCHA
NATS_PUBLISH_FAILED
DATABASE_FAILED
UNKNOWN_ERROR
```

Retryable by default:

```text
NAVIGATION_TIMEOUT
FETCH_FAILED
SCREENSHOT_FAILED
STORAGE_UPLOAD_FAILED
NATS_PUBLISH_FAILED
DATABASE_FAILED
UNKNOWN_ERROR
```

Non-retryable by default:

```text
INVALID_URL
UNSUPPORTED_SOURCE
DUPLICATE_CHECK_ID
DUPLICATE_LISTING_URL
BLOCKED_OR_CAPTCHA
```

`BLOCKED_OR_CAPTCHA` may be configurable, but for OTOMOTO it should be non-retryable by default.

---

## 22. Acceptance Criteria

The implementation is considered complete when:

```text
1. The service is named AutoVerdict.CarListingCrawler.
2. The service receives CarCheckRequestedMessage from NATS JetStream.
3. The service validates ListingUrl before browser navigation.
4. The service does not perform authentication.
5. The service does not attempt CAPTCHA or anti-bot bypassing.
6. The service opens a public OTOMOTO listing page with Playwright.
7. The service extracts minimal data: title, canonical URL, current URL, meta description, language, and block/CAPTCHA detection flag.
8. The service captures a full-page screenshot where possible.
9. The service falls back to a large viewport screenshot if full-page screenshot fails.
10. The service uploads the screenshot to SeaweedFS.
11. The service stores technical job state and result references in PostgreSQL.
12. The service publishes CarCheckCrawledMessage to NATS JetStream.
13. The service implements CheckId idempotency.
14. The service implements configurable TTL-based ListingUrl deduplication.
15. The service implements per-domain rate limiting.
16. The service handles timeout, invalid URL, unsupported source, and blocked/CAPTCHA page scenarios.
17. The service can run in Docker on Ubuntu Server 24.04.
18. The service provides structured logs.
19. The service provides health checks if supported by the existing architecture.
```

---

## 23. Explicit Restrictions

The implementation agent must not implement:

```text
- CAPTCHA bypassing;
- anti-bot bypassing;
- Google login automation;
- social login automation;
- saved Google cookies usage;
- scraping of closed account-only data;
- automatic login or OAuth flows;
- stealth browser plugins;
- browser fingerprint spoofing;
- proxy rotation intended to bypass restrictions;
- aggressive mass crawling.
```

The crawler must only work with publicly accessible pages.

---

## 24. Recommended MVP Plan

### Stage 1

```text
- NATS consumer and publisher;
- PostgreSQL crawler job state;
- URL validation;
- Playwright page opening;
- minimal page-level extraction;
- screenshot capture;
- SeaweedFS upload;
- result event publishing.
```

### Stage 2

```text
- rate limiting;
- deduplication;
- retry and backoff;
- block/CAPTCHA detection;
- health checks.
```

### Stage 3

```text
- full OTOMOTO parser;
- richer normalization;
- content hash;
- screenshot fallback strategy;
- metrics and tracing.
```

### Stage 4

```text
- support for additional listing sources through source-specific handlers;
- source-specific configurations;
- better scheduling and backpressure;
- admin/debug endpoint if needed by the existing platform.
```

---

## 25. Key Architecture Decision

Recommended final design:

```text
NATS JetStream = queue and event integration.
PostgreSQL = crawler job state, idempotency, history, and diagnostics.
SeaweedFS = screenshot storage.
Playwright = public page rendering without authorization.
Browser lifecycle = one Browser instance per process, one BrowserContext per job, one Page per job.
Rate limiting = conservative per-domain limits, especially for otomoto.pl.
```

This approach keeps the crawler reliable, understandable, and suitable for a pet project that demonstrates good data architecture rather than fragile anti-bot bypassing.

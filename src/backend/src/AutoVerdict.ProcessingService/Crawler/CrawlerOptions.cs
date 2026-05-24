namespace AutoVerdict.ProcessingService.Crawler;

public sealed class CrawlerOptions
{
    public const string SectionName = "Crawler";

    public int DefaultTimeoutSeconds { get; set; } = 60;
    public int MaxAttempts { get; set; } = 3;
    public Dictionary<string, SourceCrawlerOptions> Sources { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class SourceCrawlerOptions
{
    public string[] AllowedDomains { get; set; } = [];
    public string[] BlockedPaths { get; set; } =
    [
        "/authentication", "/account", "/myaccount", "/login",
        "/oauth", "/api", "/ajax", "/contact"
    ];
    public int MaxConcurrency { get; set; } = 1;
    public int MinDelaySeconds { get; set; } = 10;
    public int MaxDelaySeconds { get; set; } = 30;
    public int MaxRequestsPerMinute { get; set; } = 3;
    public bool CaptchaIsRetryable { get; set; }
}

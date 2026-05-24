using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace AutoVerdict.ProcessingService.Crawler;

public sealed class DomainRateLimiter(
    IOptions<CrawlerOptions> options,
    ILogger<DomainRateLimiter> logger)
{
    private readonly CrawlerOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastRequests = new(StringComparer.OrdinalIgnoreCase);

    public async Task WaitAsync(string domain, CancellationToken cancellationToken)
    {
        var opts = GetOptions(domain);
        var sem = _semaphores.GetOrAdd(domain, _ => new SemaphoreSlim(opts.MaxConcurrency, opts.MaxConcurrency));

        await sem.WaitAsync(cancellationToken);

        if (_lastRequests.TryGetValue(domain, out var last))
        {
            var range = Math.Max(0, opts.MaxDelaySeconds - opts.MinDelaySeconds);
            var jitter = range > 0 ? Random.Shared.NextDouble() * range : 0;
            var requiredDelay = TimeSpan.FromSeconds(opts.MinDelaySeconds + jitter);
            var elapsed = DateTimeOffset.UtcNow - last;

            if (elapsed < requiredDelay)
            {
                var wait = requiredDelay - elapsed;
                logger.LogDebug(
                    "Rate limit: waiting {WaitMs:F0}ms before crawling {Domain}.",
                    wait.TotalMilliseconds, domain);
                await Task.Delay(wait, cancellationToken);
            }
        }
    }

    public void Release(string domain)
    {
        _lastRequests[domain] = DateTimeOffset.UtcNow;

        if (_semaphores.TryGetValue(domain, out var sem))
            sem.Release();
    }

    private SourceCrawlerOptions GetOptions(string domain)
    {
        foreach (var (_, opts) in _options.Sources)
        {
            if (opts.AllowedDomains.Any(d =>
                domain.Equals(d, StringComparison.OrdinalIgnoreCase) ||
                domain.EndsWith("." + d, StringComparison.OrdinalIgnoreCase)))
                return opts;
        }

        return new SourceCrawlerOptions();
    }
}

using Anthropic.Exceptions;
using Microsoft.Extensions.Logging;

namespace AutoVerdict.Infrastructure.AI;

/// <summary>
/// In-call retry for transient Anthropic API failures. Wraps a single stage
/// call; pipeline-level (cross-attempt) retries are handled by the NATS
/// consumer with its own backoff.
/// </summary>
public sealed class AiRetryPolicy(TimeProvider clock, ILogger<AiRetryPolicy> logger)
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan[] Delays = [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(8)];

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await operation(ct);
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsTransient(ex, ct))
            {
                var baseDelay = Delays[attempt - 1];
                var jitterFactor = 1 + ((Random.Shared.NextDouble() * 0.4) - 0.2); // ±20 %
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * jitterFactor);
                logger.LogWarning(ex,
                    "Transient AI API error (attempt {Attempt}/{Max}); retrying in {Delay}.",
                    attempt, MaxAttempts, delay);
                await Task.Delay(delay, clock, ct);
            }
        }
    }

    internal static bool IsTransient(Exception ex, CancellationToken ct) => ex switch
    {
        AnthropicRateLimitException => true,
        AnthropicServiceException => true,
        AnthropicIOException => true,
        HttpRequestException => true,
        TaskCanceledException when !ct.IsCancellationRequested => true,
        _ => false,
    };
}

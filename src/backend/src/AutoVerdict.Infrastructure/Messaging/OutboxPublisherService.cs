using System.Text;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace AutoVerdict.Infrastructure.Messaging;

public sealed class OutboxPublisherService(
    IOptions<NatsOptions> natsOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisherService> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private const int MaxRetries = 10;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var nats = new NatsConnection(new NatsOpts { Url = natsOptions.Value.Url });
        await nats.ConnectAsync();

        var js = new NatsJSContext(nats);

        logger.LogInformation("Outbox publisher started, polling every {Interval}s.", PollInterval.TotalSeconds);

        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PublishPendingAsync(js, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox poll cycle failed.");
            }
        }
    }

    private async Task PublishPendingAsync(NatsJSContext js, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return;

        logger.LogDebug("Publishing {Count} outbox message(s).", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(message.Payload);
                await js.PublishAsync(message.Subject, bytes, cancellationToken: ct);
                message.ProcessedAt = DateTimeOffset.UtcNow;
                message.Error = null;

                logger.LogInformation(
                    "Published outbox message {Id} to {Subject}.", message.Id, message.Subject);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;

                logger.LogError(ex,
                    "Failed to publish outbox message {Id} to {Subject} (attempt {Attempt}/{Max}).",
                    message.Id, message.Subject, message.RetryCount, MaxRetries);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}

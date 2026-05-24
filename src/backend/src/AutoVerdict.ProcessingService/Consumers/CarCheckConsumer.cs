using AutoVerdict.Application.Checks;
using AutoVerdict.Contracts.Configuration;
using AutoVerdict.Contracts.Messages;
using AutoVerdict.Infrastructure.Messaging;
using AutoVerdict.ProcessingService.Crawler;
using AutoVerdict.ProcessingService.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.Serializers.Json;

namespace AutoVerdict.ProcessingService.Consumers;

public sealed class CarCheckConsumer(
    IOptions<NatsOptions> natsOptions,
    CarCheckAnalysisPipeline pipeline,
    IServiceScopeFactory scopeFactory,
    ILogger<CarCheckConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var url = natsOptions.Value.Url;
        logger.LogInformation("Connecting to NATS at {Url}", url);

        await using var nats = new NatsConnection(new NatsOpts { Url = url });
        await nats.ConnectAsync();

        var js = new NatsJSContext(nats);

        var consumer = await js.CreateOrUpdateConsumerAsync(
            NatsSubjects.Streams.CarChecks,
            new ConsumerConfig(NatsSubjects.Consumers.ProcessingService)
            {
                FilterSubject = NatsSubjects.CarCheckRequested,
                DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                AckPolicy = ConsumerConfigAckPolicy.Explicit,
                MaxDeliver = 5,
                AckWait = TimeSpan.FromMinutes(2),
            },
            stoppingToken);

        logger.LogInformation("NATS JetStream consumer ready on {Subject}", NatsSubjects.CarCheckRequested);

        // In-memory fast-path idempotency; DB-level check is authoritative.
        var processedIds = new HashSet<Guid>();

        await foreach (NatsJSMsg<CarCheckRequestedMessage?> msg
            in consumer.ConsumeAsync<CarCheckRequestedMessage?>(
                serializer: NatsJsonSerializer<CarCheckRequestedMessage?>.Default,
                cancellationToken: stoppingToken))
        {
            if (msg.Data is not { } data)
            {
                logger.LogWarning("Received empty message on {Subject}, acking.", msg.Subject);
                await msg.AckAsync(cancellationToken: stoppingToken);
                continue;
            }

            if (processedIds.Contains(data.CheckId))
            {
                logger.LogWarning("Duplicate check {CheckId} (redelivery), acking without reprocessing.", data.CheckId);
                await msg.AckAsync(cancellationToken: stoppingToken);
                continue;
            }

            try
            {
                logger.LogInformation(
                    "Processing check {CheckId} for listing {ListingUrl}.",
                    data.CheckId, data.ListingUrl);

                // ── Crawl phase ───────────────────────────────────────────────
                CrawlResult crawlResult;
                await using (var crawlScope = scopeFactory.CreateAsyncScope())
                {
                    var orchestrator = crawlScope.ServiceProvider.GetRequiredService<CrawlerOrchestrator>();
                    crawlResult = await orchestrator.CrawlAsync(data, stoppingToken);
                }

                // Publish crawled event (best effort — do not fail the job if this fails)
                try
                {
                    var crawledMsg = BuildCrawledMessage(data, crawlResult);
                    await js.PublishAsync(
                        NatsSubjects.CarCheckCrawled,
                        crawledMsg,
                        serializer: NatsJsonSerializer<CarCheckCrawledMessage>.Default,
                        cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Check {CheckId}: failed to publish crawled event.", data.CheckId);
                }

                // Already processed in a prior run — just ack
                if (crawlResult.Status == "SkippedDuplicate")
                {
                    processedIds.Add(data.CheckId);
                    await msg.AckAsync(cancellationToken: stoppingToken);
                    continue;
                }

                // Non-retryable crawl failure — record and ack
                if (!crawlResult.IsSuccess && !crawlResult.IsRetryableError)
                {
                    await using var failScope = scopeFactory.CreateAsyncScope();
                    var resultService = failScope.ServiceProvider.GetRequiredService<ICarCheckResultService>();
                    await resultService.RecordFailureAsync(
                        data.CheckId, crawlResult.ErrorMessage ?? crawlResult.Status, stoppingToken);

                    var failedMsg = new CarCheckFailedMessage(
                        data.CheckId, data.UserId,
                        crawlResult.ErrorMessage ?? crawlResult.Status,
                        DateTimeOffset.UtcNow);
                    await js.PublishAsync(
                        NatsSubjects.CarCheckFailed,
                        failedMsg,
                        serializer: NatsJsonSerializer<CarCheckFailedMessage>.Default,
                        cancellationToken: stoppingToken);

                    processedIds.Add(data.CheckId);
                    await msg.AckAsync(cancellationToken: stoppingToken);
                    continue;
                }

                // Retryable crawl failure — nak so JetStream can redeliver
                if (!crawlResult.IsSuccess)
                {
                    logger.LogWarning(
                        "Check {CheckId}: retryable crawl error {Code}, naking.",
                        data.CheckId, crawlResult.ErrorCode);
                    await msg.NakAsync(cancellationToken: stoppingToken);
                    continue;
                }

                // ── AI analysis phase ─────────────────────────────────────────
                var aiResult = await pipeline.ExecuteAsync(data, crawlResult.ParseResult!, stoppingToken);

                await using (var successScope = scopeFactory.CreateAsyncScope())
                {
                    var resultService = successScope.ServiceProvider.GetRequiredService<ICarCheckResultService>();
                    await resultService.RecordSuccessAsync(data.CheckId, aiResult, stoppingToken);
                }

                var completed = new CarCheckCompletedMessage(
                    data.CheckId, data.UserId, aiResult.Report, DateTimeOffset.UtcNow);

                await js.PublishAsync(
                    NatsSubjects.CarCheckCompleted,
                    completed,
                    serializer: NatsJsonSerializer<CarCheckCompletedMessage>.Default,
                    cancellationToken: stoppingToken);

                processedIds.Add(data.CheckId);
                await msg.AckAsync(cancellationToken: stoppingToken);

                logger.LogInformation("Check {CheckId} completed and persisted.", data.CheckId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process check {CheckId}.", data.CheckId);

                try
                {
                    await using var errScope = scopeFactory.CreateAsyncScope();
                    var resultService = errScope.ServiceProvider.GetRequiredService<ICarCheckResultService>();
                    await resultService.RecordFailureAsync(data.CheckId, ex.Message, stoppingToken);

                    var failed = new CarCheckFailedMessage(
                        data.CheckId, data.UserId, ex.Message, DateTimeOffset.UtcNow);

                    await js.PublishAsync(
                        NatsSubjects.CarCheckFailed,
                        failed,
                        serializer: NatsJsonSerializer<CarCheckFailedMessage>.Default,
                        cancellationToken: stoppingToken);
                }
                catch (Exception innerEx)
                {
                    logger.LogError(innerEx, "Failed to record failure for check {CheckId}.", data.CheckId);
                }

                await msg.NakAsync(cancellationToken: stoppingToken);
            }
        }
    }

    private static CarCheckCrawledMessage BuildCrawledMessage(
        CarCheckRequestedMessage request,
        CrawlResult result)
    {
        ScreenshotInfo? screenshot = result.ScreenshotObjectKey is not null
            ? new ScreenshotInfo(
                result.ScreenshotBucket ?? "",
                result.ScreenshotObjectKey,
                result.ScreenshotContentType ?? "image/png",
                result.ScreenshotSizeBytes ?? 0,
                PublicUrl: null)
            : null;

        CrawlerError? error = result.ErrorCode is not null
            ? new CrawlerError(result.ErrorCode, result.ErrorMessage ?? "", result.IsRetryable ?? false)
            : null;

        return new CarCheckCrawledMessage(
            request.CheckId,
            request.UserId,
            request.ListingUrl,
            request.RequestedAt,
            DateTimeOffset.UtcNow,
            result.Source ?? "",
            result.Status,
            result.RawData,
            result.NormalizedData,
            screenshot,
            error);
    }
}

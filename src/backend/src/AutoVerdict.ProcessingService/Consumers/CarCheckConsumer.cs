using AutoVerdict.Application.AI;
using AutoVerdict.Application.Checks;
using AutoVerdict.Contracts.Configuration;
using AutoVerdict.Contracts.Messages;
using AutoVerdict.Infrastructure.Messaging;
using AutoVerdict.ProcessingService.Crawler;
using AutoVerdict.ProcessingService.Pipeline;
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
        logger.LogInformation("Connecting to NATS at {Url}", natsOptions.Value.Url);

        await using var nats = new NatsConnection(new NatsOpts { Url = natsOptions.Value.Url });
        await nats.ConnectAsync();

        var js = new NatsJSContext(nats);
        var consumer = await CreateConsumerAsync(js, stoppingToken);

        logger.LogInformation("NATS JetStream consumer ready on {Subject}", NatsSubjects.CarCheckRequested);

        // In-memory fast-path idempotency; DB-level check is authoritative.
        // var processedIds = new HashSet<Guid>();

        await foreach (var msg in consumer.ConsumeAsync<CarCheckRequestedMessage?>(
            serializer: NatsJsonSerializer<CarCheckRequestedMessage?>.Default,
            cancellationToken: stoppingToken))
        {
            if (msg.Data is not { } data)
            {
                logger.LogWarning("Received empty message on {Subject}, acking.", msg.Subject);
                await msg.AckAsync(cancellationToken: stoppingToken);
                continue;
            }

            // TODO: Review and refactor later
            // if (processedIds.Contains(data.CheckId))
            // {
            //     logger.LogWarning("Duplicate check {CheckId} (redelivery), acking without reprocessing.", data.CheckId);
            //     await msg.AckAsync(cancellationToken: stoppingToken);
            //     continue;
            // }

            var (shouldAck, markProcessed) = await ProcessMessageAsync(data, js, stoppingToken);

            // if (markProcessed) processedIds.Add(data.CheckId);
            if (shouldAck) await msg.AckAsync(cancellationToken: stoppingToken);
            else await msg.NakAsync(cancellationToken: stoppingToken);
        }
    }

    private async Task<(bool shouldAck, bool markProcessed)> ProcessMessageAsync(
        CarCheckRequestedMessage data,
        NatsJSContext js,
        CancellationToken ct)
    {
        try
        {
            logger.LogInformation(
                "Processing check {CheckId} for listing {ListingUrl}.",
                data.CheckId, data.ListingUrl);

            var crawlResult = await CrawlAsync(data, ct);
            await PublishCrawledEventAsync(js, data, crawlResult, ct);

            if (crawlResult.Status == "SkippedDuplicate")
                return (shouldAck: true, markProcessed: true);

            if (!crawlResult.IsSuccess && !crawlResult.IsRetryableError)
            {
                await RecordAndPublishFailureAsync(js, data, crawlResult.ErrorMessage ?? crawlResult.Status, ct);
                return (shouldAck: true, markProcessed: true);
            }

            if (!crawlResult.IsSuccess)
            {
                logger.LogWarning(
                    "Check {CheckId}: retryable crawl error {Code}, naking.",
                    data.CheckId, crawlResult.ErrorCode);
                return (shouldAck: false, markProcessed: false);
            }

            var aiResult = await pipeline.ExecuteAsync(data, crawlResult.ParseResult!, ct);
            await RecordAndPublishSuccessAsync(js, data, aiResult, ct);

            logger.LogInformation("Check {CheckId} completed and persisted.", data.CheckId);
            return (shouldAck: true, markProcessed: true);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process check {CheckId}.", data.CheckId);
            await TryRecordAndPublishFailureAsync(js, data, ex.Message, ct);
            return (shouldAck: false, markProcessed: false);
        }
    }

    private async Task<CrawlResult> CrawlAsync(CarCheckRequestedMessage data, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<CrawlerOrchestrator>();
        return await orchestrator.CrawlAsync(data, ct);
    }

    private async Task PublishCrawledEventAsync(
        NatsJSContext js,
        CarCheckRequestedMessage request,
        CrawlResult result,
        CancellationToken ct)
    {
        try
        {
            await js.PublishAsync(
                NatsSubjects.CarCheckCrawled,
                BuildCrawledMessage(request, result),
                serializer: NatsJsonSerializer<CarCheckCrawledMessage>.Default,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Check {CheckId}: failed to publish crawled event.", request.CheckId);
        }
    }

    private async Task RecordAndPublishFailureAsync(
        NatsJSContext js,
        CarCheckRequestedMessage data,
        string reason,
        CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var resultService = scope.ServiceProvider.GetRequiredService<ICarCheckResultService>();
        await resultService.RecordFailureAsync(data.CheckId, reason, ct);

        await js.PublishAsync(
            NatsSubjects.CarCheckFailed,
            new CarCheckFailedMessage(data.CheckId, data.UserId, reason, DateTimeOffset.UtcNow),
            serializer: NatsJsonSerializer<CarCheckFailedMessage>.Default,
            cancellationToken: ct);
    }

    private async Task RecordAndPublishSuccessAsync(
        NatsJSContext js,
        CarCheckRequestedMessage data,
        AiAnalysisResult aiResult,
        CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var resultService = scope.ServiceProvider.GetRequiredService<ICarCheckResultService>();
        await resultService.RecordSuccessAsync(data.CheckId, aiResult, ct);

        await js.PublishAsync(
            NatsSubjects.CarCheckCompleted,
            new CarCheckCompletedMessage(data.CheckId, data.UserId, aiResult.Report, DateTimeOffset.UtcNow),
            serializer: NatsJsonSerializer<CarCheckCompletedMessage>.Default,
            cancellationToken: ct);
    }

    private async Task TryRecordAndPublishFailureAsync(
        NatsJSContext js,
        CarCheckRequestedMessage data,
        string reason,
        CancellationToken ct)
    {
        try
        {
            await RecordAndPublishFailureAsync(js, data, reason, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record failure for check {CheckId}.", data.CheckId);
        }
    }

    private static async Task<INatsJSConsumer> CreateConsumerAsync(NatsJSContext js, CancellationToken ct) =>
        await js.CreateOrUpdateConsumerAsync(
            NatsSubjects.Streams.CarChecks,
            new ConsumerConfig(NatsSubjects.Consumers.ProcessingService)
            {
                FilterSubject = NatsSubjects.CarCheckRequested,
                DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                AckPolicy = ConsumerConfigAckPolicy.Explicit,
                MaxDeliver = 5,
                AckWait = TimeSpan.FromMinutes(2),
            },
            ct);

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

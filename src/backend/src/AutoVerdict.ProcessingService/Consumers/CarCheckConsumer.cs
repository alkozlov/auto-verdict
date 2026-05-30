using AutoVerdict.Application.Checks;
using AutoVerdict.Contracts.Configuration;
using AutoVerdict.Contracts.Enums;
using AutoVerdict.Contracts.Messages;
using AutoVerdict.Infrastructure.Messaging;
using AutoVerdict.Infrastructure.Persistence;
using AutoVerdict.ProcessingService.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.Serializers.Json;

namespace AutoVerdict.ProcessingService.Consumers;

public sealed class CarCheckConsumer(
    IOptions<NatsOptions> natsOptions,
    ICarCheckPipeline pipeline,
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

            var (shouldAck, _) = await ProcessMessageAsync(data, js, stoppingToken);
            if (shouldAck) await msg.AckAsync(cancellationToken: stoppingToken);
            else await msg.NakAsync(cancellationToken: stoppingToken);
        }
    }

    private async Task<(bool shouldAck, bool processed)> ProcessMessageAsync(
        CarCheckRequestedMessage data,
        NatsJSContext js,
        CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Processing check {CheckId}.", data.CheckId);

            if (await IsAlreadyCompletedAsync(data.CheckId, ct))
            {
                logger.LogInformation(
                    "Check {CheckId} is already completed; acknowledging duplicate message without reprocessing.",
                    data.CheckId);
                return (shouldAck: true, processed: false);
            }

            var storageKey = await pipeline.ExecuteAsync(data, ct);
            await RecordAndPublishSuccessAsync(js, data, storageKey, ct);

            logger.LogInformation("Check {CheckId} completed.", data.CheckId);
            return (shouldAck: true, processed: true);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process check {CheckId}.", data.CheckId);
            await TryRecordAndPublishFailureAsync(js, data, ex.Message, ct);
            return (shouldAck: false, processed: false);
        }
    }

    private async Task<bool> IsAlreadyCompletedAsync(Guid checkId, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.CarChecks
            .AsNoTracking()
            .AnyAsync(c => c.CheckId == checkId && c.Status == CarCheckStatus.Completed, ct);
    }

    private async Task RecordAndPublishSuccessAsync(
        NatsJSContext js,
        CarCheckRequestedMessage data,
        string storageKey,
        CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var resultService = scope.ServiceProvider.GetRequiredService<ICarCheckResultService>();
        await resultService.RecordSuccessAsync(data.CheckId, storageKey, ct);

        await js.PublishAsync(
            NatsSubjects.CarCheckCompleted,
            new CarCheckCompletedMessage(data.CheckId, data.UserId, storageKey, DateTimeOffset.UtcNow),
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
            await using var scope = scopeFactory.CreateAsyncScope();
            var resultService = scope.ServiceProvider.GetRequiredService<ICarCheckResultService>();
            await resultService.RecordFailureAsync(data.CheckId, reason, ct);

            await js.PublishAsync(
                NatsSubjects.CarCheckFailed,
                new CarCheckFailedMessage(data.CheckId, data.UserId, reason, DateTimeOffset.UtcNow),
                serializer: NatsJsonSerializer<CarCheckFailedMessage>.Default,
                cancellationToken: ct);
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
                AckWait = TimeSpan.FromMinutes(10),
            },
            ct);
}

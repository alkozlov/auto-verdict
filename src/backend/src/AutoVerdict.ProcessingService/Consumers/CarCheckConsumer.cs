using AutoVerdict.Contracts.Configuration;
using AutoVerdict.Contracts.Messages;
using AutoVerdict.ProcessingService.Configuration;
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

        // In-memory idempotency guard — per-process lifetime only.
        // Replace with DB-based check once CarCheck entity is persisted (av-020).
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
                    "Processing check {CheckId} for vehicle {VehicleIdentifier}.",
                    data.CheckId, data.VehicleIdentifier);

                var result = await pipeline.ExecuteAsync(data, stoppingToken);

                var completed = new CarCheckCompletedMessage(
                    data.CheckId,
                    data.UserId,
                    result.Report,
                    DateTimeOffset.UtcNow);

                await js.PublishAsync(
                    NatsSubjects.CarCheckCompleted,
                    completed,
                    serializer: NatsJsonSerializer<CarCheckCompletedMessage>.Default,
                    cancellationToken: stoppingToken);

                processedIds.Add(data.CheckId);
                await msg.AckAsync(cancellationToken: stoppingToken);

                logger.LogInformation("Check {CheckId} completed successfully.", data.CheckId);
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
                    var failed = new CarCheckFailedMessage(
                        data.CheckId,
                        data.UserId,
                        ex.Message,
                        DateTimeOffset.UtcNow);

                    await js.PublishAsync(
                        NatsSubjects.CarCheckFailed,
                        failed,
                        serializer: NatsJsonSerializer<CarCheckFailedMessage>.Default,
                        cancellationToken: stoppingToken);
                }
                catch (Exception pubEx)
                {
                    logger.LogError(pubEx, "Failed to publish failure message for check {CheckId}.", data.CheckId);
                }

                await msg.NakAsync(cancellationToken: stoppingToken);
            }
        }
    }
}

using AutoVerdict.Contracts.Configuration;
using AutoVerdict.Contracts.Messages;
using AutoVerdict.ProcessingService.Configuration;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.Serializers.Json;

namespace AutoVerdict.ProcessingService.Consumers;

public sealed class CarCheckConsumer(
    IOptions<NatsOptions> natsOptions,
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

        // In-memory idempotency guard — replaced by DB check in av-019
        // when the CarCheck entity and its repository are available.
        var processedIds = new HashSet<Guid>();

        await foreach (NatsJSMsg<CarCheckRequestedMessage?> msg
            in consumer.ConsumeAsync<CarCheckRequestedMessage?>(
                serializer: NatsJsonSerializer<CarCheckRequestedMessage?>.Default,
                cancellationToken: stoppingToken))
        {
            if (msg.Data is not { } data)
            {
                logger.LogWarning("Received empty or malformed message on {Subject}, acking.", msg.Subject);
                await msg.AckAsync(cancellationToken: stoppingToken);
                continue;
            }

            if (processedIds.Contains(data.CheckId))
            {
                logger.LogWarning("Duplicate check {CheckId} received (redelivery), skipping.", data.CheckId);
                await msg.AckAsync(cancellationToken: stoppingToken);
                continue;
            }

            try
            {
                logger.LogInformation(
                    "Received check request {CheckId} for vehicle {VehicleIdentifier} (user {UserId}).",
                    data.CheckId, data.VehicleIdentifier, data.UserId);

                // TODO av-019: download document from S3, invoke IAiAnalysisProvider,
                //              persist report, publish CarCheckCompletedMessage or CarCheckFailedMessage.

                processedIds.Add(data.CheckId);
                await msg.AckAsync(cancellationToken: stoppingToken);

                logger.LogInformation("Check {CheckId} acknowledged.", data.CheckId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process check {CheckId}, nacking for redelivery.", data.CheckId);
                await msg.NakAsync(cancellationToken: stoppingToken);
            }
        }
    }
}

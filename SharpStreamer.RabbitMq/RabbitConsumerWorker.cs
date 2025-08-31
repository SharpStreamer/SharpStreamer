using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Stream.Client;
using SharpStreamer.Abstractions;
using SharpStreamer.Abstractions.Exceptions;
using SharpStreamer.Abstractions.Services.Abstractions;

namespace SharpStreamer.RabbitMq;

public class RabbitConsumerWorker(
    ILogger<RabbitConsumerWorker> logger,
    ILogger<StreamSystem> streamSystemLogger,
    IOptions<RabbitConfig> rabbitConfig,
    IMetadataService metadataService,
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider
    ) : BackgroundService
{
    private readonly ConcurrentDictionary<string, Stopwatch> _offsetCommitTimersForEachPartition = new();
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StreamSystem streamSystem = await CreateStreamSystem();
        await InitializeRabbitMq(streamSystem);
        List<string> consumerGroups = metadataService.GetAllConsumerGroups();
        RegisterConsumers(consumerGroups, streamSystem, stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void RegisterConsumers(List<string> consumerGroups, StreamSystem streamSystem, CancellationToken cancellationToken)
    {
        foreach (string consumerGroup in consumerGroups)
        {
            RegisterConsumerForTopics(consumerGroup, rabbitConfig.Value.ConsumeTopics, streamSystem, cancellationToken);
        }
    }

    private void RegisterConsumerForTopics(string consumerGroup, List<RabbitTopic> topicsToBeConsumed, StreamSystem streamSystem, CancellationToken cancellationToken)
    {
        foreach (RabbitTopic topic in topicsToBeConsumed)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            {
                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
                IEventsRepository eventsRepository = scope.ServiceProvider.GetRequiredService<IEventsRepository>();
                ISuperStreamConsumer consumer = await streamSystem.CreateSuperStreamConsumer(
                    new RawSuperStreamConsumerConfig(topic.Name)
                    {
                        Reference = consumerGroup,

                        // Single Active Consumer - ensures only one consumer per partition
                        IsSingleActiveConsumer = true,

                        // Offset specification - only used for NEW consumer groups
                        OffsetSpec = await GetOffsets(topic.Name, consumerGroup, topic.PartitionsCount, streamSystem),

                        // Message handler - handles messages from ALL partitions
                        MessageHandler = async (superStream, consumer, context, message) =>
                        {
                            await HandleMessage(message, context, superStream, consumer, consumerGroup, eventsRepository);
                        },
                    });

                SemaphoreSlim disposeConsumerSemaphoreSlim = new SemaphoreSlim(0, 1);
                await using CancellationTokenRegistration registration =
                    cancellationToken.Register(() => disposeConsumerSemaphoreSlim.Release());
                await disposeConsumerSemaphoreSlim.WaitAsync(CancellationToken.None);
                await consumer.Close();
            }, CancellationToken.None);
        }
    }

    private async Task HandleMessage(Message message, MessageContext context, string topicWithPartition, RawConsumer consumer, string consumerGroup, IEventsRepository eventsRepository)
    {
        try
        {
            EventEntity @event = new EventEntity()
            {
                EventKey = ExtractValueFromHeaders(message, "event_key"),
                Id = Guid.Parse(ExtractValueFromHeaders(message, "idempotency_id")),
                TryCount = 0,
                Flags = 0,
                SentAt = DateTimeOffset.Parse(ExtractValueFromHeaders(message, "sent_at")),
                UpdatedAt = null,
                Timestamp = timeProvider.GetUtcNow(),
                ConsumerGroup = consumerGroup,
                EventBody = Encoding.UTF8.GetString(message.Data.Contents),
                Partition = topicWithPartition,
            }
            .WithHeaders(message.ApplicationProperties.ToDictionary(p => p.Key, p => p.Value.ToString() ?? ""));

            await eventsRepository.CreateIfNotExists(@event);

            await CommitOffset(context, topicWithPartition, consumer, false);
        }
        catch (SharpStreamerException ex)
        {
            logger.LogError(ex, "SharpStreamer error happened while processing message");

            await CommitOffset(context, topicWithPartition, consumer, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unknown error happened while processing message message content: {0}", Encoding.UTF8.GetString(message.Data.Contents));

            await CommitOffset(context, topicWithPartition, consumer, true);
        }
    }

    private static string ExtractValueFromHeaders(Message message, string headerName)
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        if (!message.ApplicationProperties.TryGetValue(headerName, out object value))
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        {
            throw new SharpStreamerException($@"Received event doesn't have ""{headerName}"" header");
        }

        return value.ToString()!;
    }

    private async Task CommitOffset(MessageContext context, string topicWithPartition, RawConsumer consumer, bool errorHappened)
    {
        bool isFirstMessage = !_offsetCommitTimersForEachPartition.ContainsKey(topicWithPartition);
        Stopwatch stopwatch = _offsetCommitTimersForEachPartition.GetOrAdd(topicWithPartition, _ =>
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            return stopwatch;
        });

        // Commits offset when error happened, consumer took first message after starting or 1 minutes goes after last commit
        if (stopwatch.Elapsed >= TimeSpan.FromMinutes(1) || isFirstMessage ||  errorHappened)
        {
            await consumer.StoreOffset(context.Offset);

            logger.LogInformation($"Offset commited for topic-partition : {topicWithPartition}. Time elapsed after last commit:  {stopwatch.Elapsed}, is first message: {isFirstMessage}, error happened : {errorHappened}");
            stopwatch.Restart();
        }
    }

    private async Task<ConcurrentDictionary<string, IOffsetType>> GetOffsets(string topicName, string consumerGroup, int topicPartitionsCount, StreamSystem streamSystem)
    {
        ConcurrentDictionary<string, IOffsetType> offsets = new ConcurrentDictionary<string, IOffsetType>();
        for (int partition = 0; partition < topicPartitionsCount; partition++)
        {
            offsets.TryAdd($"{topicName}-{partition}", new OffsetTypeOffset(await GetExistingOffset(streamSystem, topicName, partition, consumerGroup)));
        }

        return offsets;
    }

    private async Task InitializeRabbitMq(StreamSystem streamSystem)
    {
        foreach (RabbitTopic topic in rabbitConfig.Value.ConsumeTopics)
        {
            await streamSystem.CreateSuperStream(
                new PartitionsSuperStreamSpec(topic.Name, topic.PartitionsCount)
                {
                    MaxAge = TimeSpan.FromMinutes(topic.RetentionTimeInMinutes),
                });
        }
    }

    private async Task<StreamSystem> CreateStreamSystem()
    {
        StreamSystem streamSystem = await StreamSystem.Create(
            new StreamSystemConfig()
            {
                UserName = rabbitConfig.Value.UserName,
                Password = rabbitConfig.Value.Password,
                VirtualHost = rabbitConfig.Value.VirtualHost,
                Endpoints = rabbitConfig.Value.Addresses.Select(addr => (EndPoint)new IPEndPoint(IPAddress.Parse(addr.Ip), addr.Port)).ToList(),
                ConnectionPoolConfig = new ConnectionPoolConfig()
                {
                    ConsumersPerConnection = 10,
                    ProducersPerConnection = 10,
                    ConnectionCloseConfig = new ConnectionCloseConfig()
                    {
                        IdleTime = TimeSpan.FromMinutes(5), // Will only work if policy is CloseWhenEmptyAndIdle.
                        Policy = ConnectionClosePolicy.CloseWhenEmptyAndIdle,
                    }
                }
            }, streamSystemLogger);
        return streamSystem;
    }

    private static async Task<ulong> GetExistingOffset(StreamSystem streamSystem, string topic, int partition, string consumerGroup)
    {
        try
        {
            ulong existingOffset = await streamSystem.QueryOffset(consumerGroup, $"{topic}-{partition}");
            return existingOffset + 1;
        }
        catch (OffsetNotFoundException)
        {
            return 0;
        }
    }
}
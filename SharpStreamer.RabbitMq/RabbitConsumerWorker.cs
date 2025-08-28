using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Stream.Client;
using SharpStreamer.Abstractions.Services.Abstractions;

namespace SharpStreamer.RabbitMq;

public class RabbitConsumerWorker(
    ILogger<RabbitConsumerWorker> logger,
    ILogger<StreamSystem> streamSystemLogger,
    IOptions<RabbitConfig> rabbitConfig,
    IMetadataService metadataService
    ) : BackgroundService
{
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
                            await HandleMessage(message, context, superStream, consumer);
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

    private static async Task HandleMessage(Message message, MessageContext context, string superStream, RawConsumer consumer)
    {
        try
        {
            // Process the message
            Console.WriteLine(Encoding.ASCII.GetString(message.Data.Contents) + " - "  + context.Offset + "from - " + superStream);
                            
            // Commit offset after successful processing
            await consumer.StoreOffset(context.Offset);
                            
            Console.WriteLine($"✓ Processed and committed offset: {context.Offset}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error processing message at offset {context.Offset}: {ex.Message}");
            // Don't commit offset on error - message will be reprocessed
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
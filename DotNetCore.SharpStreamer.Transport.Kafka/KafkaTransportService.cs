using System.Text;
using Confluent.Kafka;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace DotNetCore.SharpStreamer.Transport.Kafka;

public class KafkaTransportService : ITransportService, IDisposable
{
    private readonly List<IProducer<string, string>> _producers;
    private readonly IOptions<KafkaOptions> _kafkaOptions;
    public KafkaTransportService(
        IOptions<KafkaOptions> kafkaOptions)
    {
        _kafkaOptions = kafkaOptions;
        ProducerConfig producerConfig = new()
        {
            BootstrapServers = kafkaOptions.Value.Servers,
            Acks = Acks.All,
            EnableIdempotence = true,
            LingerMs = 50, // Waits for 50ms to collect messages and sent to kafka with batch request
            CompressionType = null,
        };
        ProducerBuilder<string, string> producerBuilder = new(producerConfig);
        _producers = [];
        for (int i = 0; i < kafkaOptions.Value.ProducersCount; i++)
        {
            _producers.Add(producerBuilder.Build());
        }
    }

    public async Task Publish(List<PublishedEvent> publishedEvents, CancellationToken cancellationToken = default)
    {
        // (index, events)
        List<(int index,List<PublishedEvent> events)> publishedEventGroupedWithEventKeys = publishedEvents
            .GroupBy(e => e.EventKey)
            .Select((g,i) => (i, g.OrderBy(e => e.Timestamp).ToList()))
            .ToList();
        await Parallel.ForEachAsync(publishedEventGroupedWithEventKeys, 
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _kafkaOptions.Value.ProducersCount, 
                CancellationToken = CancellationToken.None
            },
            async (indexToEventsMapping, token) =>
            {
                int producerIndex = indexToEventsMapping.index % _kafkaOptions.Value.ProducersCount;
                IProducer<string, string> producer = _producers[producerIndex];
                foreach (PublishedEvent publishedEvent in indexToEventsMapping.events)
                {
                    Headers eventHeaders = new Headers();
                    eventHeaders.Add(nameof(PublishedEvent.Id), Encoding.UTF8.GetBytes(publishedEvent.Id.ToString()));
                    eventHeaders.Add(nameof(PublishedEvent.SentAt), Encoding.UTF8.GetBytes(publishedEvent.SentAt.ToString()));
                    await producer.ProduceAsync(
                        publishedEvent.Topic,
                        new Message<string, string>()
                        {
                            Key = publishedEvent.EventKey,
                            Value = publishedEvent.Content,
                            Headers = eventHeaders,
                        },
                        token);
                }
            });
    }

    public void Dispose()
    {
        foreach (IProducer<string, string> producer in _producers)
        {
            producer.Dispose();
        }
    }
}
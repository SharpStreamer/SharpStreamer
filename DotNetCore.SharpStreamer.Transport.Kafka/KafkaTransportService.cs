using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Services.Abstractions;

namespace DotNetCore.SharpStreamer.Transport.Kafka;

public class KafkaTransportService : ITransportService
{
    public Task Publish(List<PublishedEvent> publishedEvents, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
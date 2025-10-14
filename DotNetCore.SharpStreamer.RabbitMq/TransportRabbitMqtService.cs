using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Services.Abstractions;

namespace DotNetCore.SharpStreamer.RabbitMq;

public class TransportRabbitMqtService : ITransportService
{
    public Task Publish(PublishedEvent publishedEvent)
    {
        throw new NotImplementedException();
    }
}
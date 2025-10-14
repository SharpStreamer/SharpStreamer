using System.Text.Json;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace DotNetCore.SharpStreamer.Transport.RabbitMq;

public class TransportRabbitMqtService(ILogger<TransportRabbitMqtService> logger) : ITransportService
{
    public Task Publish(List<PublishedEvent> publishedEvents, CancellationToken cancellationToken)
    {
        logger.LogInformation("Published event: {data}", JsonSerializer.Serialize(publishedEvents));
        return Task.CompletedTask;
    }
}
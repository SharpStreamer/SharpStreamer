using System.Text;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Services.Abstractions;
using RabbitMQ.Client;

namespace DotNetCore.SharpStreamer.Transport.RabbitMq;

public class RabbitTransportService(
    RabbitConnectionProvider rabbitConnectionProvider)
    : ITransportService
{
    public async Task Publish(List<PublishedEvent> publishedEvents, CancellationToken cancellationToken = default)
    {
        IConnection connection = await rabbitConnectionProvider.GetConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        foreach (PublishedEvent publishedEvent in publishedEvents)
        {
            await PublishEvent(publishedEvent, channel, cancellationToken);
        }
    }

    private static async Task PublishEvent(
        PublishedEvent publishedEvent,
        IChannel channel,
        CancellationToken cancellationToken)
    {
        BasicProperties properties = new BasicProperties()
        {
            Persistent = true,
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            Headers = new Dictionary<string, object?>
            {
                { nameof(PublishedEvent.Id), publishedEvent.Id.ToString() },
                { nameof(PublishedEvent.SentAt), publishedEvent.SentAt.ToString() }
            },
        };
        ReadOnlyMemory<byte> body = Encoding.UTF8.GetBytes(publishedEvent.Content);
        await channel.BasicPublishAsync(
            new PublicationAddress(ExchangeType.Fanout, publishedEvent.Topic, publishedEvent.EventKey),
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
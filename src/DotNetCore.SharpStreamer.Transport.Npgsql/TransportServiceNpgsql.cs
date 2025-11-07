using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace DotNetCore.SharpStreamer.Transport.Npgsql;

public class TransportServiceNpgsql(
    IEventsRepository eventsRepository,
    ITimeService timeService,
    IOptions<SharpStreamerOptions> sharpStreamerOptions) : ITransportService
{
    public async Task Publish(List<PublishedEvent> publishedEvents, CancellationToken cancellationToken)
    {
        DateTimeOffset currentTime = timeService.GetUtcNow();
        List<ReceivedEvent> receivedEvents = publishedEvents.Select((publishedEvent,index) =>
            new ReceivedEvent()
            {
                Id = publishedEvent.Id,
                Group = sharpStreamerOptions.Value.ConsumerGroup,
                ErrorMessage = null,
                UpdateTimestamp = null,
                Partition = null,
                Topic = publishedEvent.Topic,
                Content = publishedEvent.Content,
                RetryCount = 0,
                SentAt = publishedEvent.SentAt,

                // Makes sure that because of fast processing, all received event won't have same Timestamp, because predecessors
                // ordering is based on Timestamp, and we should be sure that in same batch, we will have correctly ordered timestamps.
                Timestamp = currentTime.AddMilliseconds(index),
                Status = EventStatus.None,
                EventKey = publishedEvent.EventKey,
            }).ToList();

        await eventsRepository.SaveConsumedEvents(receivedEvents, cancellationToken);
    }
}
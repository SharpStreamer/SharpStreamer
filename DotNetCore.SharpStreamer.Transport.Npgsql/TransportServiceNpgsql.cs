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
        List<ReceivedEvent> receivedEvents = publishedEvents.Select(p =>
            new ReceivedEvent()
            {
                Id = p.Id,
                Group = sharpStreamerOptions.Value.ConsumerGroup,
                ErrorMessage = null,
                UpdateTimestamp = null,
                Partition = null,
                Topic = p.Topic,
                Content = p.Content,
                RetryCount = 0,
                SentAt = p.SentAt,
                Timestamp = timeService.GetUtcNow(),
                Status = EventStatus.None,
                EventKey = p.EventKey,
            }).ToList();

        await eventsRepository.SaveConsumedEvents(receivedEvents, cancellationToken);
    }
}
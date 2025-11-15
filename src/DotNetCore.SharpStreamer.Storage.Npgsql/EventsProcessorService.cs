using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Storage.Npgsql.Abstractions;
using Medallion.Threading;
using Microsoft.Extensions.Options;

namespace DotNetCore.SharpStreamer.Storage.Npgsql;

internal class EventsProcessorService(
    IEventsRepository eventsRepository,
    IEventProcessor eventProcessor,
    IOptions<SharpStreamerOptions> options,
    IDistributedLockProvider lockProvider) : IEventsProcessor
{
    public async Task ProcessEvents()
    {
        List<ReceivedEvent> events;
        await using (IDistributedSynchronizationHandle _ = await lockProvider.AcquireLockAsync(
                         $"{options.Value.ConsumerGroup}-{nameof(EventsProcessorService)}",
                         TimeSpan.FromMinutes(2),
                         CancellationToken.None))
        {
            events = await eventsRepository.GetAndMarkEventsForProcessing(CancellationToken.None);
        }

        Dictionary<Guid, EventStatus> processedEvents = new Dictionary<Guid, EventStatus>();
        foreach (ReceivedEvent receivedEvent in events)
        {
            (Guid id, EventStatus newStatus, string? exceptionMessage) =
                await eventProcessor.ProcessEvent(receivedEvent, processedEvents);

            const string escapeCharForExceptionMessage = "'";
            if (id != Guid.Empty)
            {
                receivedEvent.Status = newStatus;
                receivedEvent.ErrorMessage = exceptionMessage?[..Math.Min(1000, exceptionMessage.Length)]?.Replace(escapeCharForExceptionMessage[0], '-'); // Takes first 1000 character only
                processedEvents.Add(id, newStatus);
                await eventsRepository.MarkPostProcessing(receivedEvent, CancellationToken.None);
            }
        }
    }
}
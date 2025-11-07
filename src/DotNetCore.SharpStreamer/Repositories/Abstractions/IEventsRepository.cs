using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;

namespace DotNetCore.SharpStreamer.Repositories.Abstractions;

public interface IEventsRepository
{
    Task<List<ReceivedEvent>> GetAndMarkEventsForProcessing(CancellationToken cancellationToken = default);

    Task MarkPostProcessing(List<ReceivedEvent> receivedEvents, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetPredecessorIds(string eventKey, DateTimeOffset time, CancellationToken cancellationToken = default);

    Task<List<PublishedEvent>> GetEventsToPublish(CancellationToken cancellationToken = default);

    Task MarkPostPublishAttempt(List<PublishedEvent> events, CancellationToken cancellationToken = default);

    Task SaveConsumedEvents(List<ReceivedEvent> receivedEvents, CancellationToken cancellationToken = default);

    Task<List<PublishedEvent>> GetProducedEventsByStatusAndElapsedTimespan(EventStatus eventStatus, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    Task<List<ReceivedEvent>> GetReceivedEventsByStatusAndElapsedTimespan(EventStatus eventStatus, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    Task DeleteProducedEventsById(List<Guid> eventIds, CancellationToken cancellationToken = default);

    Task DeleteReceivedEventsById(List<Guid> eventIds, CancellationToken cancellationToken = default);
}
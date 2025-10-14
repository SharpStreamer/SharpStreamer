using DotNetCore.SharpStreamer.Entities;

namespace DotNetCore.SharpStreamer.Repositories.Abstractions;

public interface IEventsRepository
{
    Task<List<ReceivedEvent>> GetAndMarkEventsForProcessing(CancellationToken cancellationToken = default);

    Task MarkPostProcessing(List<ReceivedEvent> receivedEvents, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetPredecessorIds(string eventKey, DateTimeOffset time, CancellationToken cancellationToken = default);

    Task<List<PublishedEvent>> GetEventsToPublish(CancellationToken cancellationToken = default);

    Task MarkPostPublishAttempt(List<PublishedEvent> events, CancellationToken cancellationToken = default);
}
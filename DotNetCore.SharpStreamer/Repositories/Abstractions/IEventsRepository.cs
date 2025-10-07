using DotNetCore.SharpStreamer.Entities;

namespace DotNetCore.SharpStreamer.Repositories.Abstractions;

public interface IEventsRepository
{
    Task<List<ReceivedEvent>> GetAndMarkEventsForProcessing(CancellationToken cancellationToken = default);

    Task MarkPostProcessing(List<ReceivedEvent> receivedEvents, CancellationToken cancellationToken = default);
}
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;

namespace DotNetCore.SharpStreamer.Storage.Npgsql.Abstractions;

internal interface IEventProcessor
{
    Task<(Guid id, EventStatus newStatus, string? exceptionMessage)> ProcessEvent(ReceivedEvent receivedEvent, Dictionary<Guid, EventStatus> processedEvents);
}
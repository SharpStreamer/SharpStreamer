using DotNetCore.SharpStreamer.Entities;

namespace DotNetCore.SharpStreamer.Services.Abstractions;

public interface IConsumerService
{
    Task SaveConsumedEvent(ReceivedEvent receivedEvent);
}
namespace SharpStreamer.Abstractions;

public interface IConsumer<in TEvent> where TEvent : IEvent
{
    Task Handle(TEvent request, CancellationToken cancellationToken = default);
}
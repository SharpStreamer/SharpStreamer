namespace SharpStreamer.Abstractions;

public interface IEventProducer
{
    Task Produce<T>(T data) where T : IEvent;

    Task ProduceDelayed<T>(T data, TimeSpan produceAfter) where T : IEvent;
}
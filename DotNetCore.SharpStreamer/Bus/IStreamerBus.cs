namespace DotNetCore.SharpStreamer.Bus;

public interface IStreamerBus
{
    Task ProduceAsync<T>(T message);

    Task ProduceDelayedAsync<T>(T message, TimeSpan delay);
}
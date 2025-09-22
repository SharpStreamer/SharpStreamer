namespace DotNetCore.SharpStreamer.Bus;

public interface IStreamerBus
{
    Task ProduceAsync<T>(T message);
}
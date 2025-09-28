namespace DotNetCore.SharpStreamer.Bus;

public interface IStreamerBus
{
    Task ProduceAsync<T>(T message, params KeyValuePair<string, string>[] headers) where T : class;

    Task ProduceDelayedAsync<T>(T message, TimeSpan delay, params KeyValuePair<string, string>[] headers) where T : class;
}
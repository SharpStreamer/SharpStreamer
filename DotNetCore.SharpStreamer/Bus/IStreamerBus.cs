namespace DotNetCore.SharpStreamer.Bus;

public interface IStreamerBus
{
    Task PublishAsync<T>(T message, params KeyValuePair<string, string>[] headers) where T : class;

    Task PublishDelayedAsync<T>(T message, TimeSpan delay, params KeyValuePair<string, string>[] headers) where T : class;
}
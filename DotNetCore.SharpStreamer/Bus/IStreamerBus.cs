namespace DotNetCore.SharpStreamer.Bus;

public interface IStreamerBus
{
    Task PublishAsync<T>(T message, string eventKey, params KeyValuePair<string, string>[] headers) where T : class;

    Task PublishDelayedAsync<T>(T message, string eventKey, TimeSpan delay, params KeyValuePair<string, string>[] headers) where T : class;
}
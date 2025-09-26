using DotNetCore.SharpStreamer.Services.Models;

namespace DotNetCore.SharpStreamer.Services.Abstractions;

public interface ICacheService
{
    void CacheConsumer(Type type);

    /// <summary>
    /// Returns metadata of consumer which consumes event with name of 'eventName'
    /// </summary>
    ConsumerMetadata? GetConsumerMetadata(string eventName);
}
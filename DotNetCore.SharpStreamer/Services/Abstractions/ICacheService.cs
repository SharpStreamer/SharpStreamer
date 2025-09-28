using DotNetCore.SharpStreamer.Services.Models;

namespace DotNetCore.SharpStreamer.Services.Abstractions;

public interface ICacheService
{
    bool TryCacheConsumer(Type type);

    /// <summary>
    /// Returns metadata of consumer which consumes event with name of 'eventName'
    /// </summary>
    ConsumerMetadata? GetConsumerMetadata(string eventName);

    PublishableEventMetadata GetOrCreatePublishableEventMetadata<T>();
}
using System.Reflection;
using DotNetCore.SharpStreamer.Bus.Attributes;
using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Services.Models;
using DotNetCore.SharpStreamer.Utils;

namespace DotNetCore.SharpStreamer.Services;

internal static class ProducerCache<T>
{
    // ReSharper disable once StaticMemberInGenericType
    internal static readonly Lazy<PublishableEventMetadata> Cache = new(() =>
    {
        Type producerType = typeof(T);
        if (!producerType.IsLegitPublishableEvent())
        {
            throw new ArgumentException("Event is not legit publishable");
        }
        PublishEventAttribute attribute = producerType.GetCustomAttribute<PublishEventAttribute>()!;
        return new PublishableEventMetadata()
        {
            EventName = attribute.EventName,
            TopicName = attribute.TopicName,
        };
    });
}
internal class CacheService : ICacheService
{
    private readonly Dictionary<string, ConsumerMetadata> _cache = new();
    public bool TryCacheConsumer(Type type)
    {
        if (!type.IsLegitConsumableEvent())
        {
            return false;
        }

        ConsumeEventAttribute consumeEventAttribute = type.GetCustomAttribute<ConsumeEventAttribute>()!;

        ConsumerMetadata consumerMetadata = new ConsumerMetadata()
        {
            EventType = type,
            NeedsToBeCheckedPredecessor = consumeEventAttribute.CheckPredecessor,
        };

        if (!_cache.TryAdd(consumeEventAttribute.EventName, consumerMetadata))
        {
            throw new ArgumentException(
                $"consumer of '{consumeEventAttribute.EventName}' was already registered. you only can register single consumer for single event.");
        }

        return true;
    }

    public ConsumerMetadata? GetConsumerMetadata(string eventName)
    {
        _cache.TryGetValue(eventName, out ConsumerMetadata? consumerMetadata);
        return consumerMetadata;
    }

    public PublishableEventMetadata GetOrCreatePublishableEventMetadata<T>()
    {
        return ProducerCache<T>.Cache.Value;
    }
}
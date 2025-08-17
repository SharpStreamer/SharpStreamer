using SharpStreamer.Abstractions.Attributes;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("SharpStreamer.RabbitMq.Tests")]

namespace SharpStreamer.RabbitMq;

public static class Cache
{
    // Key : {eventName}:{consumerGroup}, Value: Consumer metadata. This field is internal because of unit tests only. it should be accessed from only Cache class and ca be marked as private.
    internal static Dictionary<string, RabbitConsumerMetadata> ConsumersMetadata = new();

    internal static void CacheConsumerMetadata(List<ConsumeEventAttribute> consumeEventAttributes, Type consumerType, Type eventType)
    {
        foreach (ConsumeEventAttribute consumeEventAttribute in consumeEventAttributes)
        {
            string key = $"{consumeEventAttribute.EventName}:{consumeEventAttribute.ConsumerGroupName}";
            bool added = Cache.ConsumersMetadata.TryAdd(
                key,
                new RabbitConsumerMetadata()
                {
                    ConsumerType = consumerType,
                    EventType = eventType,
                });
            if (!added)
            {
                HandleCacheAlreadyExists(eventType, key);
            }
        }
    }

    private static void HandleCacheAlreadyExists(Type eventType, string key)
    {
        Type existingEvent = ConsumersMetadata[key].EventType;
        if (existingEvent == eventType)
        {
            return;
        }
        throw new ArgumentException(
            $"You cannot have two Events with same EventName and ConsumerGroupName. These events are: {eventType.FullName} and {existingEvent.FullName}");
    }
}
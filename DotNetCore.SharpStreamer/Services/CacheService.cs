using System.Reflection;
using DotNetCore.SharpStreamer.Bus.Attributes;
using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Services.Models;
using Mediator;

namespace DotNetCore.SharpStreamer.Services;

internal class CacheService : ICacheService
{
    private readonly Dictionary<string, ConsumerMetadata> _cache = new();
    private static readonly Type IRequestType = typeof(IBaseRequest);
    public void CacheConsumer(Type type)
    {
        ConsumeEventAttribute? consumeEventAttribute = type.GetCustomAttribute<ConsumeEventAttribute>();

        if (consumeEventAttribute is null)
        {
            throw new ArgumentException($"Consume event attribute not found for type: {type.FullName}");
        }

        if (!type.IsAssignableTo(IRequestType))
        {
            throw new ArgumentException($"Type is not derived from mediator's IRequest class");
        }

        ConsumerMetadata consumerMetadata = new ConsumerMetadata()
        {
            EventType = type,
        };

        if (!_cache.TryAdd(consumeEventAttribute.EventName, consumerMetadata))
        {
            throw new ArgumentException(
                $"consumer of '{consumeEventAttribute.EventName}' was already registered. you only can register single consumer for single event.");
        }
    }

    public ConsumerMetadata? GetConsumerMetadata(string eventName)
    {
        _cache.TryGetValue(eventName, out ConsumerMetadata? consumerMetadata);
        return consumerMetadata;
    }
}
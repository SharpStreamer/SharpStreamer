using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using SharpStreamer.Abstractions.Attributes;
using SharpStreamer.Abstractions.Services.Abstractions;
using SharpStreamer.Abstractions.Services.Models;
[assembly: InternalsVisibleTo("SharpStreamer.RabbitMq")]
[assembly: InternalsVisibleTo("SharpStreamer.Abstractions.Tests")]

namespace SharpStreamer.Abstractions.Services;

internal class MetadataService : IMetadataService
{
    private readonly Dictionary<string, ConsumerMetadata> _consumersMetadata = new();

    /// <summary>
    /// Key : {eventName}:{consumerGroup}, Value: Consumer metadata.
    /// It should be accessed from only Cache class and ca be marked as private.
    /// </summary>
    public IReadOnlyDictionary<string, ConsumerMetadata> ConsumersMetadata => _consumersMetadata.AsReadOnly();

    public void AddServicesAndCache(IServiceCollection services, Assembly[] assemblies)
    {
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableToAny(typeof(IConsumer<>)))
            .AsImplementedInterfaces(type => type.IsGenericType && 
                                             type.GetGenericTypeDefinition() == typeof(IConsumer<>) &&
                                             HasConsumerAttributeAndCache(type.GetGenericArguments()[0], type))
            .WithTransientLifetime());
    }

    public List<string> GetAllConsumerGroups() => _consumersMetadata.Select(x => x.Key.Split(':')[1]).Distinct().ToList();

    private bool HasConsumerAttributeAndCache(Type eventType, Type consumerType)
    {
        List<ConsumeEventAttribute> consumeEventAttributes = eventType.GetCustomAttributes<ConsumeEventAttribute>().ToList();

        if (consumeEventAttributes.Count == 0)
        {
            return false;
        }

        CacheConsumerMetadata(consumeEventAttributes, consumerType, eventType);
        return true;
    }

    private void CacheConsumerMetadata(List<ConsumeEventAttribute> consumeEventAttributes, Type consumerType, Type eventType)
    {
        foreach (ConsumeEventAttribute consumeEventAttribute in consumeEventAttributes)
        {
            string key = $"{consumeEventAttribute.EventName}:{consumeEventAttribute.ConsumerGroupName}";
            bool added = _consumersMetadata.TryAdd(
                key,
                new ConsumerMetadata()
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

    private void HandleCacheAlreadyExists(Type eventType, string key)
    {
        Type existingEvent = _consumersMetadata[key].EventType;
        if (existingEvent == eventType)
        {
            return;
        }
        throw new ArgumentException(
            $"You cannot have two Events with same EventName and ConsumerGroupName. These events are: {eventType.FullName} and {existingEvent.FullName}");
    }
}
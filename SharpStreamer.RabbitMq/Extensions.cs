using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharpStreamer.Abstractions;
using SharpStreamer.Abstractions.Attributes;

namespace SharpStreamer.RabbitMq;

public static class Extensions
{
    public static IServiceCollection AddSharpStreamerRabbitMq(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableToAny(typeof(IConsumer<>)))
            .AsImplementedInterfaces(type => type.IsGenericType && 
                                             type.GetGenericTypeDefinition() == typeof(IConsumer<>) &&
                                             HasConsumerAttributeAndCache(type.GetGenericArguments()[0], type))
            .WithTransientLifetime());
        return services;
    }

    private static bool HasConsumerAttributeAndCache(Type eventType, Type consumerType)
    {
        List<ConsumeEventAttribute> consumeEventAttributes = eventType.GetCustomAttributes<ConsumeEventAttribute>().ToList();

        if (consumeEventAttributes.Count == 0)
        {
            return false;
        }

        Cache.CacheConsumerMetadata(consumeEventAttributes, consumerType, eventType);
        return true;
    }
}
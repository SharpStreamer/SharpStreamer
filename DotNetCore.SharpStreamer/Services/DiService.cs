using System.Reflection;
using System.Runtime.CompilerServices;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Services.Abstractions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: InternalsVisibleTo("DotNetCore.SharpStreamer.Transport.Kafka")]

namespace DotNetCore.SharpStreamer.Services;

internal class DiService(ICacheService cacheService)
{
    private const string CoreSettingsName = "Core";
    internal static string? ConfigurationSectionName = null;
    public IServiceCollection AddSharpStreamer(IServiceCollection services, string configurationSection, params Assembly[] addFromAssemblies)
    {
        ConfigurationSectionName = configurationSection;
        bool mediatorExists = services.Any(x => x.ServiceType == typeof(IMediator));
        if (!mediatorExists)
        {
            throw new InvalidOperationException(
                $"Mediator doesn't exist. please implement IMediator interface before calling {nameof(AddSharpStreamer)})");
        }
        services.AddSingleton<ICacheService>(cacheService);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IIdGenerator, UlidGenerator>();
        services.AddSingleton<ITimeService, TimeService>();

        services.AddOptions<SharpStreamerOptions>()
            .BindConfiguration($"{configurationSection}:{CoreSettingsName}")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        CacheConsumableEventsMetadata(addFromAssemblies);
        return services;
    }

    private void CacheConsumableEventsMetadata(Assembly[] addFromAssemblies)
    {
        foreach (Assembly assembly in addFromAssemblies)
        {
            CacheConsumableEventsMetadata(assembly);
        }
    }

    private void CacheConsumableEventsMetadata(Assembly assembly)
    {
        IEnumerable<Type> consumableEventTypes = assembly.DefinedTypes;
        foreach (Type consumableEventType in consumableEventTypes)
        {
            cacheService.TryCacheConsumer(consumableEventType);
        }
    }
}
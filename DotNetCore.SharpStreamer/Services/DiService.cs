using System.Reflection;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetCore.SharpStreamer.Services;

internal class DiService(ICacheService cacheService)
{
    private const string CoreSettingsName = "Core";
    public IServiceCollection AddSharpStreamer(IServiceCollection services, string configurationSection, params Assembly[] addFromAssemblies)
    {
        services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssemblies(addFromAssemblies);
            options.Lifetime = ServiceLifetime.Transient;
        });
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
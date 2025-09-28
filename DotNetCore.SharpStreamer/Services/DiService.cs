using System.Reflection;
using DotNetCore.SharpStreamer.Bus.Attributes;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.SharpStreamer.Services;

public class DiService(ICacheService cacheService)
{
    public IServiceCollection AddSharpStreamer(IServiceCollection services, params Assembly[] addFromAssemblies)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Transient;
            options.Assemblies = addFromAssemblies.Select(assembly => (AssemblyReference)assembly).ToList();
        });
        services.AddSingleton<ICacheService>(cacheService);
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
        IEnumerable<Type> consumableEventTypes = assembly.GetTypes();
        foreach (Type consumableEventType in consumableEventTypes)
        {
            cacheService.TryCacheConsumer(consumableEventType);
        }
    }
}
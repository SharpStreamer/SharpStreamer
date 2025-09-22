using System.Reflection;
using System.Runtime.CompilerServices;
using DotNetCore.SharpStreamer.Services;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("DotNetCore.SharpStreamer.EfCore.Npgsql")]
[assembly: InternalsVisibleTo("DotNetCore.SharpStreamer.RabbitMq")]

namespace DotNetCore.SharpStreamer;

public static class SharpStreamerExtensions
{
    /// <summary>
    /// Should be called from any integration. Adds core of the library
    /// </summary>
    public static IServiceCollection AddSharpStreamer(this IServiceCollection services, params  Assembly[] addFromAssemblies)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Transient;
            options.Assemblies = addFromAssemblies.Select(assembly => (AssemblyReference)assembly).ToList();
        });
        services.AddSingleton<ICacheService, CacheService>();
        return services;
    }

    /// <summary>
    /// Should be called from specific integrations. That is the reason why it is internal
    /// </summary>
    internal static IServiceCollection AddSharpStreamerForBus(this IServiceCollection services)
    {
        return services;
    }

    /// <summary>
    /// Should be called from specific integrations. That is the reason why it is internal
    /// </summary>
    internal static IServiceCollection AddSharpStreamerForStorage(this IServiceCollection services)
    {
        return services;
    }
}
using System.Reflection;
using System.Runtime.CompilerServices;
using DotNetCore.SharpStreamer.Services;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("DotNetCore.SharpStreamer.EfCore.Npgsql")]
[assembly: InternalsVisibleTo("DotNetCore.SharpStreamer.RabbitMq")]

namespace DotNetCore.SharpStreamer;

public static class SharpStreamerExtensions
{
    /// <summary>
    /// Adds mediator and caches specific metadata
    /// </summary>
    public static IServiceCollection AddSharpStreamer(this IServiceCollection services, string configurationSection, params  Assembly[] addFromAssemblies)
    {
        CacheService cacheService = new CacheService();
        DiService diService = new DiService(cacheService);
        return diService.AddSharpStreamer(services, configurationSection, addFromAssemblies);
    }
}
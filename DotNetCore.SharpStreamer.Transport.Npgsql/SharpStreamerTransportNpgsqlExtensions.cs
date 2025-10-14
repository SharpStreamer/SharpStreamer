using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.SharpStreamer.Transport.Npgsql;

public static class SharpStreamerTransportNpgsqlExtensions
{
    public static IServiceCollection AddSharpStreamerTransportNpgsql(this IServiceCollection services)
    {
        services.AddScoped<ITransportService, TransportServiceNpgsql>();
        return services;
    }
}
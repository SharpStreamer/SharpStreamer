using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.SharpStreamer.Transport.Sqlite;

public static class SharpStreamerTransportSqliteExtensions
{
    public static IServiceCollection AddSharpStreamerTransportSqlite(this IServiceCollection services)
    {
        services.AddScoped<ITransportService, TransportServiceSqlite>();
        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharpStreamer.Abstractions;

namespace SharpStreamer.EntityFrameworkCore.Npgsql;

public static class Extensions
{
    public static IServiceCollection AddSharpPersistenceNpgSql<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddHostedService<DbInitializerService<TDbContext>>();
        services.AddScoped<IEventsRepository, EventsRepository<TDbContext>>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
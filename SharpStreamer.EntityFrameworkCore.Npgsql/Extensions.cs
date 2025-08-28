using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SharpStreamer.EntityFrameworkCore.Npgsql;

public static class Extensions
{
    public static IServiceCollection AddSharpPersistenceNpgSql<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddHostedService<DbInitializerService<TDbContext>>();
        return services;
    }
}
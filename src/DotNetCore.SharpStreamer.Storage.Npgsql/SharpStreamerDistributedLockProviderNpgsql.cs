using Medallion.Threading;
using Medallion.Threading.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.SharpStreamer.Storage.Npgsql;

public class SharpStreamerDistributedLockProviderNpgsql<TDbContext>(IServiceScopeFactory serviceScopeFactory) : IDistributedLockProvider
    where TDbContext : DbContext
{
    private readonly IDistributedLockProvider _synchronizationProvider =
        new PostgresDistributedSynchronizationProvider(GetConnectionString(serviceScopeFactory));

    public IDistributedLock CreateLock(string name)
    {
        return _synchronizationProvider.CreateLock(name);
    }

    private static string GetConnectionString(
        IServiceScopeFactory scopeFactory)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        return dbContext.Database.GetDbConnection().ConnectionString;
    }
}
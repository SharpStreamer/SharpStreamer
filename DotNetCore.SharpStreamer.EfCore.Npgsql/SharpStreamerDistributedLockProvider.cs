using Medallion.Threading;
using Medallion.Threading.Postgres;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.SharpStreamer.EfCore.Npgsql;

public class SharpStreamerDistributedLockProvider<TDbContext>(TDbContext dbContext) : IDistributedLockProvider
    where TDbContext : DbContext
{
    private readonly IDistributedLockProvider _synchronizationProvider =
        new PostgresDistributedSynchronizationProvider(dbContext.Database.GetDbConnection().ConnectionString); 
    public IDistributedLock CreateLock(string name)
    {
        return _synchronizationProvider.CreateLock(name);
    }
}
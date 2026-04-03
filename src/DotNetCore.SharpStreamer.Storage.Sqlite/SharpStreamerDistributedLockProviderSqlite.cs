using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.SharpStreamer.Storage.Sqlite;

public class SharpStreamerDistributedLockProviderSqlite<TDbContext>(IServiceScopeFactory serviceScopeFactory) : IDistributedLockProvider
    where TDbContext : DbContext
{
    private readonly IDistributedLockProvider _synchronizationProvider =
        CreateProvider(serviceScopeFactory);

    public IDistributedLock CreateLock(string name)
    {
        return _synchronizationProvider.CreateLock(name);
    }

    private static IDistributedLockProvider CreateProvider(IServiceScopeFactory scopeFactory)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        string connectionString = dbContext.Database.GetDbConnection().ConnectionString;

        SqliteConnectionStringBuilder builder = new(connectionString);
        string dataSource = builder.DataSource;

        string lockDir;
        if (string.IsNullOrEmpty(dataSource) || dataSource == ":memory:" || dataSource.StartsWith("file::memory:"))
        {
            lockDir = Path.Combine(Path.GetTempPath(), "sharp_streamer_locks");
        }
        else
        {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
            lockDir = Path.Combine(directory ?? Path.GetTempPath(), ".sharp_streamer_locks");
        }

        Directory.CreateDirectory(lockDir);
        return new FileDistributedSynchronizationProvider(new DirectoryInfo(lockDir));
    }
}

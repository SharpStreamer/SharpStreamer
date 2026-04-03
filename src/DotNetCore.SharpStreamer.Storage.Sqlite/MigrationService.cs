using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Storage.Sqlite.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.SharpStreamer.Storage.Sqlite;

public class MigrationService<TDbContext> : IMigrationService where TDbContext : DbContext
{
    private readonly ILogger<MigrationService<TDbContext>> _logger;
    private readonly Lock _migrationRunnerLock = new();
    private bool _isMigrationApplied = false;
    private readonly string _connectionString;

    public MigrationService(
        IServiceScopeFactory scopeFactory,
        ILogger<MigrationService<TDbContext>> logger)
    {
        _logger = logger;
        using IServiceScope scope = scopeFactory.CreateScope();
        DbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();
        _connectionString = context.Database.GetConnectionString()!;
    }

    public void Migrate()
    {
        if (_isMigrationApplied)
        {
            return;
        }

        lock (_migrationRunnerLock)
        {
            if (_isMigrationApplied)
            {
                return;
            }

            DbContextOptions<SqliteDbContext> options = new DbContextOptionsBuilder<SqliteDbContext>()
                .UseSqlite(_connectionString)
                .Options;
            using DbContext context = new SqliteDbContext(options);
            context.Database.EnsureCreated();
            _isMigrationApplied = true;
            _logger.LogInformation("SharpStreamer SQLite migrated");
        }
    }
}

using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Storage.Npgsql.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.SharpStreamer.Storage.Npgsql;

public class MigrationService<TDbContext> : IMigrationService where TDbContext : DbContext
{
    private readonly Lock _migrationRunnerLock = new();
    private bool _isMigrationApplied = false;
    private readonly string _connectionString;

    public MigrationService(IServiceScopeFactory scopeFactory)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        DbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();
        _connectionString = context.Database.GetConnectionString()!;
    }

    public void Migrate()
    {
        if (_isMigrationApplied) // This check ensures that after one call there is no need to take lock again.
        {
            return;
        }

        lock (_migrationRunnerLock)
        {
            if (_isMigrationApplied)
            {
                return;
            }

            DbContextOptions<NpgsqlDbContext> options = new DbContextOptionsBuilder<NpgsqlDbContext>()
                .UseNpgsql(_connectionString)
                .Options;
            using DbContext context = new NpgsqlDbContext(options);
            context.Database.Migrate();
            _isMigrationApplied = true;
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SharpStreamer.EntityFrameworkCore.Npgsql;

public class DbInitializerService<TDbContext>(IServiceScopeFactory serviceScopeFactory) : BackgroundService
    where TDbContext : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string dbInitializerScriptPath = Path.Combine(AppContext.BaseDirectory, "db_init.sql");
        string script = await File.ReadAllTextAsync(dbInitializerScriptPath, stoppingToken);
        await RunDbInitScript(script);
    }

    private async Task RunDbInitScript(string script)
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(sql: script);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SharpStreamer.EntityFrameworkCore.Npgsql;

public class DbInitializerService<TDbContext>(IServiceScopeFactory serviceScopeFactory) : BackgroundService
    where TDbContext : DbContext
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetCore.SharpStreamer.Storage.Npgsql.Jobs;

public class ProducedEventsCleaner(
    ITimeService timeService,
    ILogger<ProducedEventsCleaner> logger,
    IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunProducedEventsCleaner();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(ProducedEventsCleaner)}");
            }
            finally
            {
                await timeService.Delay(TimeSpan.FromMilliseconds(1000), CancellationToken.None);
            }
        }
    }

    private async Task RunProducedEventsCleaner()
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IEventsRepository eventsRepository = scope.ServiceProvider.GetRequiredService<IEventsRepository>();
    }
}
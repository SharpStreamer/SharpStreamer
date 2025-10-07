using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.SharpStreamer.EfCore.Npgsql.Jobs;

public class EventsProcessor(
    IDistributedLockProvider lockProvider,
    ITimeService timeService,
    IOptions<SharpStreamerOptions> options,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<EventsProcessor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<Task> processorTasks = new List<Task>();

        for (int i = 0; i < options.Value.ProcessorThreadCount; i++)
        {
            processorTasks.Add(Task.Run(async () => await RunProcessor(stoppingToken), stoppingToken));
        }

        await Task.WhenAll(processorTasks);
    }

    private async Task RunProcessor(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
                IEventsRepository eventsRepository = scope.ServiceProvider.GetRequiredService<IEventsRepository>();

                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in processing events");
                await timeService.Delay(TimeSpan.FromMilliseconds(200), stoppingToken);
            }
        }
    }
}
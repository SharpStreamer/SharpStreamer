using System.Runtime.CompilerServices;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Storage.Npgsql.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: InternalsVisibleTo("Storage.Npgsql.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace DotNetCore.SharpStreamer.Storage.Npgsql.Jobs;

internal class EventsProcessor(
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
                await RunProcessor();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in processing events");
            }
            finally
            {
                await timeService.Delay(TimeSpan.FromMilliseconds(1000), CancellationToken.None);
            }
        }
    }

    private async Task RunProcessor()
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        IEventsProcessor eventsProcessor = scope.ServiceProvider.GetRequiredService<IEventsProcessor>();
        await eventsProcessor.ProcessEvents();
    }
}
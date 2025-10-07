using DotNetCore.SharpStreamer.Entities;
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

                List<ReceivedEvent> eventsToProcess;
                await using (IDistributedSynchronizationHandle _ = await lockProvider.AcquireLockAsync(
                                 $"{options.Value.BaseConsumerGroupName}-{nameof(EventsProcessor)}",
                                 TimeSpan.FromMinutes(2),
                                 CancellationToken.None))
                {
                    eventsToProcess = await eventsRepository.GetAndMarkEventsForProcessing(CancellationToken.None);
                }

                foreach (ReceivedEvent receivedEvent in eventsToProcess)
                {
                    ProcessEvent(receivedEvent, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in processing events");
                await timeService.Delay(TimeSpan.FromMilliseconds(200), stoppingToken);
            }
        }
    }

    private void ProcessEvent(ReceivedEvent receivedEvent, CancellationToken none)
    {
        try
        {
            // check if, needs to be checked predecessor and if yes, check it. if check fails throw exception
            // Process event
        }
        catch (Exception e)
        {
            // if exception happens, set exception details in event headers. also log it.
        }
    }
}
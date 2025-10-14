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

public class EventsPublisher(
    IOptions<SharpStreamerOptions> options,
    ITimeService timeService,
    IDistributedLockProvider lockProvider,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<EventsPublisher> logger) : BackgroundService
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
        IEventsRepository eventsRepository = scope.ServiceProvider.GetRequiredService<IEventsRepository>();
        ITransportService transportService = scope.ServiceProvider.GetRequiredService<ITransportService>();

        await using (IDistributedSynchronizationHandle _ = await lockProvider.AcquireLockAsync(
                         $"{options.Value.ConsumerGroup}-{nameof(EventsPublisher)}",
                         TimeSpan.FromMinutes(2),
                         CancellationToken.None))
        {
            List<PublishedEvent> publishedEvents = await eventsRepository.GetEventsToPublish(CancellationToken.None);
            foreach (PublishedEvent publishedEvent in publishedEvents)
            {
                
            }

            if (publishedEvents.Any())
            {
                await eventsRepository.MarkPostPublishAttempt(publishedEvents, CancellationToken.None);
            }
        }
    }
}
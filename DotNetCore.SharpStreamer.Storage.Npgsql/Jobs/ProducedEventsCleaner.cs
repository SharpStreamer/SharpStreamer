using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.SharpStreamer.Storage.Npgsql.Jobs;

public class ProducedEventsCleaner(
    ITimeService timeService,
    ILogger<ProducedEventsCleaner> logger,
    IDistributedLockProvider distributedLockProvider,
    IOptions<SharpStreamerOptions> sharpStreamerOptions,
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
        await using (IDistributedSynchronizationHandle _ = await distributedLockProvider.AcquireLockAsync(
                         $"{sharpStreamerOptions.Value.ConsumerGroup}-{nameof(ProducedEventsCleaner)}",
                         TimeSpan.FromMinutes(2),
                         CancellationToken.None));

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IEventsRepository eventsRepository = scope.ServiceProvider.GetRequiredService<IEventsRepository>();

        List<PublishedEvent> publishedEvents = await eventsRepository.GetProducedEventsByStatusAndElapsedTimespan(
            EventStatus.Succeeded,
            TimeSpan.FromDays(1));

        if (publishedEvents.Any())
        {
            await eventsRepository.DeleteProducedEventsById(publishedEvents.Select(e => e.Id).ToList());
        }
    }
}
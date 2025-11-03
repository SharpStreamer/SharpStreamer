using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetCore.SharpStreamer.Storage.Npgsql.Jobs;

public class ProcessedEventsCleaner(
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
                await RunProcessedEventsCleaner();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(ProcessedEventsCleaner)}");
            }
            finally
            {
                await timeService.Delay(TimeSpan.FromMilliseconds(1000), CancellationToken.None);
            }
        }
    }

    private async Task RunProcessedEventsCleaner()
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IEventsRepository eventsRepository = scope.ServiceProvider.GetRequiredService<IEventsRepository>();

        List<ReceivedEvent> receivedEvents = await eventsRepository.GetReceivedEventsByStatusAndElapsedTimespan(
            EventStatus.Succeeded,
            TimeSpan.FromDays(1));

        if (receivedEvents.Any())
        {
            await eventsRepository.DeleteReceivedEventsById(receivedEvents.Select(e => e.Id).ToList());
        }
    }
}
using System.Text.Json;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Services.Models;
using DotNetCore.SharpStreamer.Utils;
using Medallion.Threading;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.SharpStreamer.EfCore.Npgsql.Jobs;

internal class EventsProcessor(
    IDistributedLockProvider lockProvider,
    ITimeService timeService,
    ICacheService cacheService,
    IOptions<SharpStreamerOptions> options,
    IServiceScopeFactory serviceScopeFactory,
    IMediator mediator,
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
                    await ProcessEvent(receivedEvent, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in processing events");
                await timeService.Delay(TimeSpan.FromMilliseconds(200), stoppingToken);
            }
        }
    }

    private async Task ProcessEvent(ReceivedEvent receivedEvent, CancellationToken none)
    {
        try
        {
            (string rawEventBody, string eventName) = GetEventBodyAndName(receivedEvent.Content);

            ConsumerMetadata? consumerMetadata = cacheService.GetConsumerMetadata(eventName);
            if (consumerMetadata is null)
            {
                return;
            }

            if (consumerMetadata.NeedsToBeCheckedPredecessor)
            {
                await EnsurePredecessorsAreProcessed(receivedEvent);
            }

            object? @event = JsonSerializer.Deserialize(
                rawEventBody,
                consumerMetadata.EventType,
                JsonExtensions.SharpStreamerJsonOptions);

            if (@event is null)
            {
                throw new ArgumentException($"Because of unknown reason deserialized event was null. event body: {rawEventBody}");
            }

            await mediator.Send(@event, CancellationToken.None);
        }
        catch (Exception e)
        {
            // if exception happens, set exception details in event headers. also log it.
        }
    }

    private Task EnsurePredecessorsAreProcessed(ReceivedEvent receivedEvent)
    {
        throw new NotImplementedException();
    }

    private static (string body, string eventName) GetEventBodyAndName(string content)
    {
        string eventBody;
        string eventName;
        using var doc = JsonDocument.Parse(content);
        JsonElement root = doc.RootElement;
        if (root.TryGetProperty("body", out JsonElement bodyElement) &&
            bodyElement.ValueKind == JsonValueKind.Object)
        {
            eventBody = bodyElement.GetRawText();
        }
        else
        {
            throw new ArgumentException($"Received events content, doesn't contain body property. content: {content}");
        }

        if (root.TryGetProperty("event_name", out JsonElement eventNameElement) &&
            bodyElement.ValueKind == JsonValueKind.String)
        {
            eventName = eventNameElement.GetString()!;
        }
        else
        {
            throw new ArgumentException($"Received events content, doesn't contain event name property. content: {content}");
        }

        return (eventBody, eventName);
    }
}
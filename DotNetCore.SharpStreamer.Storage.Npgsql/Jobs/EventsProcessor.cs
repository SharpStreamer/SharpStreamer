using System.Text.Json;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Services.Models;
using DotNetCore.SharpStreamer.Utils;
using Medallion.Threading;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.SharpStreamer.Storage.Npgsql.Jobs;

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

        List<ReceivedEvent> events;
        await using (IDistributedSynchronizationHandle _ = await lockProvider.AcquireLockAsync(
                         $"{options.Value.ConsumerGroup}-{nameof(EventsProcessor)}",
                         TimeSpan.FromMinutes(2),
                         CancellationToken.None))
        {
            events = await eventsRepository.GetAndMarkEventsForProcessing(CancellationToken.None);
        }

        Dictionary<Guid, EventStatus> processedEvents = new Dictionary<Guid, EventStatus>();
        foreach (ReceivedEvent receivedEvent in events)
        {
            (Guid id, EventStatus newStatus, string? exceptionMessage) =
                await ProcessEvent(receivedEvent, eventsRepository, processedEvents);

            const string escapeCharForExceptionMessage = "'";
            if (id != Guid.Empty)
            {
                receivedEvent.Status = newStatus;
                receivedEvent.ErrorMessage = exceptionMessage?[..Math.Min(1000, exceptionMessage.Length)]?.Replace(escapeCharForExceptionMessage[0], '-'); // Takes first 1000 character only
                processedEvents.Add(id, newStatus);
            }
        }

        if (events.Any())
        {
            await eventsRepository.MarkPostProcessing(events, CancellationToken.None);
        }
    }

    private async Task<(Guid id, EventStatus newStatus, string? exceptionMessage)> ProcessEvent
        (ReceivedEvent receivedEvent, IEventsRepository eventsRepository, Dictionary<Guid, EventStatus> processedEvents)
    {
        ConsumerMetadata? consumerMetadata = null;
        try
        {
            (string rawEventBody, string eventName) = GetEventBodyAndName(receivedEvent.Content);

            consumerMetadata = cacheService.GetConsumerMetadata(eventName);
            if (consumerMetadata is null)
            {
                return (receivedEvent.Id, EventStatus.Succeeded, "Event handler metadata was not found. maybe you don't have it registered");
            }

            if (consumerMetadata.NeedsToBeCheckedPredecessor)
            {
                await EnsurePredecessorsAreProcessed(receivedEvent, eventsRepository, processedEvents, CancellationToken.None);
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

            logger.LogInformation($"{consumerMetadata.EventType.Name} was handled successfully.");
            return (receivedEvent.Id, EventStatus.Succeeded, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"{consumerMetadata?.EventType.Name ?? "Unknown event"} was handled unsuccessfully.");
            return (receivedEvent.Id, EventStatus.Failed, ex.Message);
        }
    }

    private async Task EnsurePredecessorsAreProcessed(
        ReceivedEvent receivedEvent,
        IEventsRepository eventsRepository,
        Dictionary<Guid, EventStatus> processedEvents,
        CancellationToken cancellationToken)
    {
        List<Guid> predecessorIds =
            await eventsRepository.GetPredecessorIds(
                receivedEvent.EventKey,
                receivedEvent.Timestamp,
                cancellationToken);
        if (predecessorIds.Any() && !PredecessorsWereProcessedInSameBatch(predecessorIds, processedEvents))
        {
            string predecessorIdsAsString = string.Join(',', predecessorIds.Select(id => id.ToString()));
            throw new ArgumentException(
                $"This received event can't be processed because there are predecessor events: {predecessorIdsAsString}");
        }
    }

    private bool PredecessorsWereProcessedInSameBatch(List<Guid> predecessorIds, Dictionary<Guid, EventStatus> processedEvents)
    {
        return predecessorIds.All(id => processedEvents.ContainsKey(id) && processedEvents[id] == EventStatus.Succeeded);
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
            eventNameElement.ValueKind == JsonValueKind.String)
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
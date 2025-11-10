using System.Text.Json;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Services.Models;
using DotNetCore.SharpStreamer.Storage.Npgsql.Abstractions;
using DotNetCore.SharpStreamer.Utils;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.SharpStreamer.Storage.Npgsql;

internal class EventProcessorService(
    ICacheService cacheService,
    IServiceScopeFactory serviceScopeFactory,
    IEventsRepository eventsRepository,
    ILogger<EventProcessorService> logger) : IEventProcessor
{
    public async Task<(Guid id, EventStatus newStatus, string? exceptionMessage)> ProcessEvent(ReceivedEvent receivedEvent, Dictionary<Guid, EventStatus> processedEvents)
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
                await EnsurePredecessorsAreProcessed(receivedEvent, processedEvents, CancellationToken.None);
            }

            object? @event = JsonSerializer.Deserialize(
                rawEventBody,
                consumerMetadata.EventType,
                JsonExtensions.SharpStreamerJsonOptions);

            if (@event is null)
            {
                throw new ArgumentException($"Because of unknown reason deserialized event was null. event body: {rawEventBody}");
            }

            await using (AsyncServiceScope processingScope = serviceScopeFactory.CreateAsyncScope())
            {
                IMediator mediator = processingScope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(@event, CancellationToken.None);
            }

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
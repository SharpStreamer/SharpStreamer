using System.Text.Json;
using DotNetCore.SharpStreamer.Bus;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Services.Models;
using DotNetCore.SharpStreamer.Utils;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.SharpStreamer.Storage.Npgsql;

internal class StreamerBusNpgsql<TDbContext>(
    TDbContext dbContext,
    ITimeService timeService,
    IIdGenerator idGenerator,
    ICacheService cacheService) : IStreamerBus
    where TDbContext : DbContext
{
    public async Task PublishAsync<T>(T message, string eventKey, params KeyValuePair<string, string>[] headers) where T : class
    {
        PublishableEventMetadata metadata = cacheService.GetOrCreatePublishableEventMetadata<T>();
        string content = GetContentAsString(message, headers, metadata);
        DateTimeOffset currentUtcTime = timeService.GetUtcNow();
        PublishedEvent publishedEvent = new()
        {
            Id = idGenerator.GenerateId(),
            Content = content,
            Timestamp = currentUtcTime,
            SentAt = currentUtcTime,
            RetryCount = 0,
            Status = EventStatus.None,
            Topic = metadata.TopicName,
            EventKey = eventKey,
        };
        await Insert(publishedEvent);
    }

    public async Task PublishDelayedAsync<T>(T message, TimeSpan delay, params KeyValuePair<string, string>[] headers)
        where T : class
    {
        PublishableEventMetadata metadata = cacheService.GetOrCreatePublishableEventMetadata<T>();
        string content = GetContentAsString(message, headers, metadata);
        DateTimeOffset currentUtcTime = timeService.GetUtcNow();
        PublishedEvent publishedEvent = new()
        {
            Id = idGenerator.GenerateId(),
            Content = content,
            Timestamp = currentUtcTime,
            SentAt = currentUtcTime.Add(delay),
            RetryCount = 0,
            Status = EventStatus.None,
            Topic = metadata.TopicName,
            EventKey = idGenerator.GenerateId().ToString(),
        };
        await Insert(publishedEvent);
    }

    private async Task Insert(PublishedEvent publishedEvent)
    {
        const string insertQuery = @"
                            INSERT INTO sharp_streamer.published_events
                            (
                                ""Id"",
                                ""Topic"",
                                ""Content"",
                                ""RetryCount"",
                                ""SentAt"",
                                ""Timestamp"",
                                ""Status"",
                                ""EventKey""
                            )
                            VALUES
                            (
                                {0},
                                {1},
                                {2}::json,
                                {3},
                                {4},
                                {5},
                                {6},
                                {7}
                            );";
        await dbContext.Database.ExecuteSqlRawAsync(
            insertQuery,
            publishedEvent.Id,
            publishedEvent.Topic,
            publishedEvent.Content,
            publishedEvent.RetryCount,
            publishedEvent.SentAt,
            publishedEvent.Timestamp,
            publishedEvent.Status,
            publishedEvent.EventKey);
    }

    private static string GetContentAsString<T>(
        T message,
        KeyValuePair<string, string>[] headers,
        PublishableEventMetadata metadata) 
        where T : class
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        Dictionary<string, object> content = new()
        {
            { "body", message },
            { "event_name", metadata.EventName },
        };
        foreach (KeyValuePair<string, string> header in headers)
        {
            content.TryAdd(header.Key, header.Value);
        }
        return JsonSerializer.Serialize(content, JsonExtensions.SharpStreamerJsonOptions);
    }
}
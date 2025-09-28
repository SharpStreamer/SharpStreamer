using System.Data;
using System.Text.Json;
using Dapper;
using DotNetCore.SharpStreamer.Bus;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Services.Models;
using DotNetCore.SharpStreamer.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DotNetCore.SharpStreamer.EfCore.Npgsql;

internal class StreamerBusNpgsql<TDbContext>(
    TDbContext dbContext,
    ITimeService timeService,
    IIdGenerator idGenerator,
    ICacheService cacheService) : IStreamerBus
    where TDbContext : DbContext
{
    public async Task PublishAsync<T>(T message, params KeyValuePair<string, string>[] headers) where T : class
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
            ExpiresAt = currentUtcTime.AddDays(1),
            RetryCount = 0,
            Status = EventStatus.None,
            Topic = metadata.TopicName,
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
            ExpiresAt = currentUtcTime.Add(delay).AddDays(1),
            RetryCount = 0,
            Status = EventStatus.None,
            Topic = metadata.TopicName,
        };
        await Insert(publishedEvent);
    }

    private async Task Insert(PublishedEvent publishedEvent)
    {
        const string insertQuery = $@"
                            INSERT INTO sharp_streamer.published_events
                            (
                                ""Id"",
                                ""Topic"",
                                ""Content"",
                                ""RetryCount"",
                                ""SentAt"",
                                ""Timestamp"",
                                ""ExpiresAt"",
                                ""Status""
                            )
                            VALUES
                            (
                                @{nameof(PublishedEvent.Id)},
                                @{nameof(PublishedEvent.Topic)},
                                @{nameof(PublishedEvent.Content)}::json,
                                @{nameof(PublishedEvent.RetryCount)},
                                @{nameof(PublishedEvent.SentAt)},
                                @{nameof(PublishedEvent.Timestamp)},
                                @{nameof(PublishedEvent.ExpiresAt)},
                                @{nameof(PublishedEvent.Status)}
                            );";
        IDbConnection dbConnection = dbContext.Database.GetDbConnection();
        IDbTransaction? dbTransaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        await dbConnection.ExecuteAsync(sql: insertQuery, param: publishedEvent, transaction: dbTransaction);
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
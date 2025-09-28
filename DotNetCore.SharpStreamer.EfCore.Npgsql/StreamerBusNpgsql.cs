using System.Text.Json;
using DotNetCore.SharpStreamer.Bus;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Utils;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.SharpStreamer.EfCore.Npgsql;

public class StreamerBusNpgsql<TDbContext>(
    TDbContext dbContext,
    TimeProvider timeProvider,
    IIdGenerator idGenerator,
    ICacheService cacheService) : IStreamerBus
    where TDbContext : DbContext
{
    public Task ProduceAsync<T>(T message, params KeyValuePair<string, string>[] headers) where T : class
    {
        string content = GetContentAsString(message, headers);
        DateTimeOffset currentUtcTime = timeProvider.GetUtcNow();
        PublishedEvent publishedEvent = new()
        {
            Id = idGenerator.GenerateId(),
            Content = content,
            Timestamp = currentUtcTime,
            SentAt = currentUtcTime,
            ExpiresAt = currentUtcTime.AddDays(1),
            RetryCount = 0,
            Status = EventStatus.None,
            Topic = null,
        };
        return Task.CompletedTask;
    }

    public Task ProduceDelayedAsync<T>(T message, TimeSpan delay, params KeyValuePair<string, string>[] headers)
        where T : class
    {
        string content = GetContentAsString(message, headers);
        return Task.CompletedTask;
    }

    private static string GetContentAsString<T>(T message, KeyValuePair<string, string>[] headers) where T : class
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        Dictionary<string, object> content = new() { { "body", message } };
        foreach (KeyValuePair<string, string> header in headers)
        {
            content.TryAdd(header.Key, header.Value);
        }
        return JsonSerializer.Serialize(content, JsonExtensions.SharpStreamerJsonOptions);
    }
}
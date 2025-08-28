using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SharpStreamer.Abstractions;

namespace SharpStreamer.EntityFrameworkCore.Npgsql;

public class EventsRepository<TDbContext>(TDbContext dbContext) : IEventsRepository
    where TDbContext : DbContext
{
    public async Task CreateIfNotExists(EventEntity eventEntity)
    {
        const string query = $@"
                            INSERT INTO sharp_streamer.received_events
                            (id, event_body, event_headers, event_key, sent_at, timestamp, updated_at, consumer_group, flags, try_count)
                            VALUES 
                            (
                            @{nameof(EventEntity.Id)},
                            @{nameof(EventEntity.EventBody)}::JSONB,
                            @{nameof(EventEntity.EventHeaders)}::JSONB,
                            @{nameof(EventEntity.EventKey)},
                            @{nameof(EventEntity.SentAt)},
                            @{nameof(EventEntity.Timestamp)},
                            @{nameof(EventEntity.UpdatedAt)},
                            @{nameof(EventEntity.ConsumerGroup)},
                            @{nameof(EventEntity.Flags)},
                            @{nameof(EventEntity.TryCount)})
                            ON CONFLICT (id) DO NOTHING;";
        await dbContext.Database.GetDbConnection().ExecuteAsync(query, param: eventEntity, dbContext.Database.CurrentTransaction?.GetDbTransaction());
    }
}
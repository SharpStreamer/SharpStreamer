using Microsoft.EntityFrameworkCore;
using SharpStreamer.Abstractions;

namespace SharpStreamer.EntityFrameworkCore.Npgsql;

public class EventsRepository<TDbContext>(TDbContext dbContext) : IEventsRepository
    where TDbContext : DbContext
{
    public async Task CreateIfNotExists(EventEntity eventEntity)
    {
        const string query = @"
                            INSERT INTO sharp_streamer.received_events
                            (id, event_body, event_headers, event_key, sent_at, timestamp, updated_at, consumer_group, flags, try_count, partition)
                            VALUES 
                            (
                                {0},
                                {1}::JSONB,
                                {2}::JSONB,
                                {3},
                                {4},
                                {5},
                                {6},
                                {7},
                                {8},
                                {9},
                                {10}
                            )
                            ON CONFLICT (id) DO NOTHING;";
        await dbContext.Database.ExecuteSqlRawAsync(sql: query,
            eventEntity.Id,
            eventEntity.EventBody,
            eventEntity.EventHeaders,
            eventEntity.EventKey,
            eventEntity.SentAt,
            eventEntity.Timestamp,
            eventEntity.UpdatedAt,
            eventEntity.ConsumerGroup,
            eventEntity.Flags,
            eventEntity.TryCount,
            eventEntity.Partition);
    }
}
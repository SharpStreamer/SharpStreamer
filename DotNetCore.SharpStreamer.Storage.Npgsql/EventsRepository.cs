using System.Data;
using Dapper;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.SharpStreamer.Storage.Npgsql;

public class EventsRepository<TDbContext>(
    TDbContext dbContext,
    ILogger<EventsRepository<TDbContext>> logger,
    IOptions<SharpStreamerOptions> sharpStreamerOptions,
    ITimeService timeService) : IEventsRepository
    where TDbContext : DbContext
{
    public async Task<List<ReceivedEvent>> GetAndMarkEventsForProcessing(CancellationToken cancellationToken = default)
    {
        DateTimeOffset cutOffTime = timeService.GetUtcNow().AddSeconds(-20);
        List<ReceivedEvent> @events = await dbContext.Set<ReceivedEvent>()
            .Where(r => r.Status == EventStatus.Failed || r.Status == EventStatus.None)
            .Where(r => r.UpdateTimestamp == null || r.UpdateTimestamp < cutOffTime)
            .Where(r => r.RetryCount < 50)
            .OrderBy(r => r.Timestamp)
            .Take(sharpStreamerOptions.Value.ProcessingBatchSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        List<Guid> eventIds = @events.Select(r => r.Id).ToList();
        await dbContext.Set<ReceivedEvent>()
            .Where(r => eventIds.Contains(r.Id))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, EventStatus.InProgress)
                    .SetProperty(r => r.RetryCount, r => r.RetryCount + 1),
                cancellationToken);

        return @events;
    }

    public async Task MarkPostProcessing(List<ReceivedEvent> receivedEvents, CancellationToken cancellationToken = default)
    {
        string statusUpdates = string.Join("\n", receivedEvents.Select(e => $"WHEN '{e.Id}' THEN {(int)e.Status}"));
        string errorMessageUpdates = string.Join("\n", receivedEvents.Select(e => $"WHEN '{e.Id}' THEN {CalculateErrorMessageValue(e)}"));
        List<Guid> ids = receivedEvents.Select(e => e.Id).ToList();

        string updateSql = $@"
                    UPDATE sharp_streamer.received_events
                    SET ""Status"" = CASE ""Id""
                                     {statusUpdates}
                                     END,
                        ""ErrorMessage"" = CASE ""Id""
                                           {errorMessageUpdates}
                                           END,
                        ""UpdateTimestamp"" = @UpdateTimestamp
                    WHERE ""Id"" = ANY (@ids);";

        logger.LogInformation($"custom query executed: {updateSql}");
        IDbConnection dbConnection = dbContext.Database.GetDbConnection();
        IDbTransaction? dbTransaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        await dbConnection.ExecuteAsync(sql: updateSql, param: new { ids = ids, UpdateTimestamp = timeService.GetUtcNow() }, transaction: dbTransaction);
    }

    public async Task<List<Guid>> GetPredecessorIds(string eventKey, DateTimeOffset time, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<ReceivedEvent>()
            .Where(r => r.EventKey == eventKey)
            .Where(r => r.Status == EventStatus.Failed || r.Status == EventStatus.None || r.Status == EventStatus.InProgress)
            .Where(r => r.Timestamp < time)
            .AsNoTracking()
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PublishedEvent>> GetEventsToPublish(CancellationToken cancellationToken = default)
    {
        DateTimeOffset currentTime = timeService.GetUtcNow();
        List<PublishedEvent> @events = await dbContext.Set<PublishedEvent>()
            .Where(r => r.Status == EventStatus.None)
            .Where(r => r.SentAt < currentTime)
            .OrderBy(r => r.SentAt)
            .Take(sharpStreamerOptions.Value.ProcessingBatchSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        List<Guid> eventIds = @events.Select(r => r.Id).ToList();
        await dbContext.Set<PublishedEvent>()
            .Where(r => eventIds.Contains(r.Id))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.RetryCount, r => r.RetryCount + 1),
                cancellationToken);
        return @events;
    }

    public async Task MarkPostPublishAttempt(List<PublishedEvent> events, CancellationToken cancellationToken = default)
    {
        List<Guid> ids = events.Select(e => e.Id).ToList();

        const string updateSql = $@"
                    UPDATE sharp_streamer.published_events
                    SET ""Status"" = 2
                    WHERE ""Id"" = ANY (@ids);";

        logger.LogInformation($"custom query executed: {updateSql}");
        IDbConnection dbConnection = dbContext.Database.GetDbConnection();
        IDbTransaction? dbTransaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        await dbConnection.ExecuteAsync(sql: updateSql, param: new { ids = ids }, transaction: dbTransaction);
    }

    public async Task SaveConsumedEvents(List<ReceivedEvent> receivedEvents, CancellationToken cancellationToken = default)
    {
        const string sql = $@"
                            INSERT INTO sharp_streamer.received_events
                            (
                                ""Id"",
                                ""Group"",
                                ""Topic"",
                                ""Content"",
                                ""RetryCount"",
                                ""SentAt"",
                                ""Timestamp"",
                                ""Status"",
                                ""ErrorMessage"",
                                ""UpdateTimestamp"",
                                ""EventKey"",
                                ""Partition""
                            )
                            VALUES 
                            (
                                @{nameof(ReceivedEvent.Id)},
                                @{nameof(ReceivedEvent.Group)},
                                @{nameof(ReceivedEvent.Topic)},
                                @{nameof(ReceivedEvent.Content)}::json,
                                @{nameof(ReceivedEvent.RetryCount)},
                                @{nameof(ReceivedEvent.SentAt)},
                                @{nameof(ReceivedEvent.Timestamp)},
                                @{nameof(ReceivedEvent.Status)},
                                @{nameof(ReceivedEvent.ErrorMessage)},
                                @{nameof(ReceivedEvent.UpdateTimestamp)},
                                @{nameof(ReceivedEvent.EventKey)},
                                @{nameof(ReceivedEvent.Partition)}
                            )
                            ON CONFLICT (""Id"") DO NOTHING;";
        logger.LogInformation($"custom query executed: {sql}");
        IDbConnection dbConnection = dbContext.Database.GetDbConnection();
        IDbTransaction? dbTransaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        await dbConnection.ExecuteAsync(sql: sql, param: receivedEvents, transaction: dbTransaction);
    }

    private static string CalculateErrorMessageValue(ReceivedEvent receivedEvent)
    {
        if (receivedEvent.ErrorMessage is null)
        {
            return "NULL";
        }

        return $"'{receivedEvent.ErrorMessage}'";
    }
}
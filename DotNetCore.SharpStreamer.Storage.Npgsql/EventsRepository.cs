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
        const string retrieveQuery = @"
                                        SELECT
                                            r.""Id"",
                                            r.""Content"",
                                            r.""ErrorMessage"",
                                            r.""EventKey"",
                                            r.""Group"",
                                            r.""Partition"",
                                            r.""RetryCount"",
                                            r.""SentAt"",
                                            r.""Status"",
                                            r.""Timestamp"",
                                            r.""Topic"",
                                            r.""UpdateTimestamp""
                                        FROM sharp_streamer.received_events AS r
                                        WHERE r.""Status"" IN (3, 0) AND (r.""UpdateTimestamp"" IS NULL OR r.""UpdateTimestamp"" < {0}) AND r.""RetryCount"" < 50
                                        ORDER BY r.""Timestamp""
                                        LIMIT {1};";

        List<ReceivedEvent> @events = await dbContext.Database.SqlQueryRaw<ReceivedEvent>(
            sql: retrieveQuery,
            cutOffTime,
            sharpStreamerOptions.Value.ProcessingBatchSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        const string updateQuery = @"
                                    UPDATE sharp_streamer.received_events AS r
                                      SET ""RetryCount"" = r.""RetryCount"" + 1,
                                          ""Status"" = 1
                                    WHERE r.""Id"" = ANY ({0});";
        if (events.Any())
        {
            List<Guid> eventIds = @events.Select(r => r.Id).ToList();
            await dbContext.Database.ExecuteSqlRawAsync(
                updateQuery,
                eventIds);
        }
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
                        ""UpdateTimestamp"" = {{0}}
                    WHERE ""Id"" = ANY ({{1}});";
        await dbContext.Database.ExecuteSqlRawAsync(updateSql, timeService.GetUtcNow(), ids);
    }

    public async Task<List<Guid>> GetPredecessorIds(string eventKey, DateTimeOffset time, CancellationToken cancellationToken = default)
    {
        const string query = @"
                            SELECT 
                                r.""Id""
                            FROM 
                                sharp_streamer.received_events AS r
                            WHERE r.""EventKey"" = {0} AND 
                                  r.""Status"" IN (3, 0, 1) AND
                                  r.""Timestamp"" < {1};";
        return await dbContext.Database
            .SqlQueryRaw<Guid>(
                sql: query,
                eventKey,
                time)
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

    public async Task<List<PublishedEvent>> GetProducedEventsByStatusAndElapsedTimespan(
        EventStatus eventStatus,
        TimeSpan timeSpan,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset cutOffTime = timeService.GetUtcNow().Subtract(timeSpan);
        return await dbContext.Set<PublishedEvent>()
            .Where(e => e.Status == eventStatus)
            .Where(e => e.SentAt <= cutOffTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReceivedEvent>> GetReceivedEventsByStatusAndElapsedTimespan(
        EventStatus eventStatus,
        TimeSpan timeSpan,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset cutOffTime = timeService.GetUtcNow().Subtract(timeSpan);
        return await dbContext.Set<ReceivedEvent>()
            .Where(e => e.Status == eventStatus)
            .Where(e => e.UpdateTimestamp <= cutOffTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteProducedEventsById(List<Guid> eventIds, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<PublishedEvent>()
            .Where(e => eventIds.Contains(e.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteReceivedEventsById(
        List<Guid> eventIds,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Set<ReceivedEvent>()
            .Where(e => eventIds.Contains(e.Id))
            .ExecuteDeleteAsync(cancellationToken);
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
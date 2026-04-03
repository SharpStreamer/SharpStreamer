using System.Text;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.SharpStreamer.Storage.Sqlite;

public class EventsRepository<TDbContext> : IEventsRepository
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly ILogger<EventsRepository<TDbContext>> _logger;
    private readonly IOptions<SharpStreamerOptions> _sharpStreamerOptions;
    private readonly ITimeService _timeService;
    public EventsRepository(
        TDbContext dbContext,
        ILogger<EventsRepository<TDbContext>> logger,
        IOptions<SharpStreamerOptions> sharpStreamerOptions,
        ITimeService timeService,
        IMigrationService migration)
    {
        migration.Migrate();
        this._dbContext = dbContext;
        this._logger = logger;
        this._sharpStreamerOptions = sharpStreamerOptions;
        this._timeService = timeService;
    }
    public async Task<List<ReceivedEvent>> GetAndMarkEventsForProcessing(CancellationToken cancellationToken = default)
    {
        DateTimeOffset cutOffTime = _timeService.GetUtcNow().AddSeconds(-20);
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
                                        FROM received_events AS r
                                        WHERE r.""Status"" IN (3, 0) AND (r.""UpdateTimestamp"" IS NULL OR r.""UpdateTimestamp"" < {0}) AND r.""RetryCount"" < 50
                                        ORDER BY r.""Timestamp"" ASC
                                        LIMIT {1};";

        List<ReceivedEvent> @events = await _dbContext.Database.SqlQueryRaw<ReceivedEvent>(
            sql: retrieveQuery,
            cutOffTime,
            _sharpStreamerOptions.Value.ProcessingBatchSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (events.Any())
        {
            List<Guid> eventIds = @events.Select(r => r.Id).ToList();
            string inClause = BuildInClauseParams(eventIds.Count, 0);
            string updateQuery = $@"
                                    UPDATE received_events
                                      SET ""RetryCount"" = ""RetryCount"" + 1,
                                          ""Status"" = 1
                                    WHERE ""Id"" IN ({inClause});";
            await _dbContext.Database.ExecuteSqlRawAsync(
                updateQuery,
                eventIds.Cast<object>().ToArray());
        }
        return @events;
    }

    public async Task MarkPostProcessing(List<ReceivedEvent> receivedEvents, CancellationToken cancellationToken = default)
    {
        DateTimeOffset currentTime = _timeService.GetUtcNow();
        foreach (ReceivedEvent receivedEvent in receivedEvents)
        {
            const string updateSql = @"
                    UPDATE received_events
                    SET ""Status"" = {0},
                        ""ErrorMessage"" = {1},
                        ""UpdateTimestamp"" = {2}
                    WHERE ""Id"" = {3};";
            await _dbContext.Database.ExecuteSqlRawAsync(updateSql, receivedEvent.Status, receivedEvent.ErrorMessage, currentTime, receivedEvent.Id);
        }
    }

    public async Task MarkPostProcessing(ReceivedEvent receivedEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string updateSql = @"
                    UPDATE received_events
                    SET ""Status"" = {0},
                        ""ErrorMessage"" = {1},
                        ""UpdateTimestamp"" = {2}
                    WHERE ""Id"" = {3};";
        await _dbContext.Database.ExecuteSqlRawAsync(updateSql, receivedEvent.Status, receivedEvent.ErrorMessage, _timeService.GetUtcNow(), receivedEvent.Id);
    }

    public async Task<List<Guid>> GetPredecessorIds(string eventKey, DateTimeOffset time, CancellationToken cancellationToken = default)
    {
        const string query = @"
                            SELECT
                                r.""Id""
                            FROM
                                received_events AS r
                            WHERE r.""EventKey"" = {0} AND
                                  r.""Status"" IN (3, 0, 1) AND
                                  r.""Timestamp"" < {1};";
        return await _dbContext.Database
            .SqlQueryRaw<Guid>(
                sql: query,
                eventKey,
                time)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PublishedEvent>> GetEventsToPublish(CancellationToken cancellationToken = default)
    {
        DateTimeOffset currentTime = _timeService.GetUtcNow();
        const string retrieveQuery = @"
                                        SELECT
                                            p.""Id"",
                                            p.""Content"",
                                            p.""EventKey"",
                                            p.""RetryCount"",
                                            p.""SentAt"",
                                            p.""Status"",
                                            p.""Timestamp"",
                                            p.""Topic""
                                        FROM published_events AS p
                                        WHERE
                                            p.""Status"" = 0 AND
                                            p.""SentAt"" < {0}
                                        ORDER BY p.""SentAt"" ASC
                                        LIMIT {1};";
        List<PublishedEvent> @events = await _dbContext.Database
            .SqlQueryRaw<PublishedEvent>(
                sql: retrieveQuery,
                currentTime,
                _sharpStreamerOptions.Value.ProcessingBatchSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        List<Guid> eventIds = @events.Select(r => r.Id).ToList();
        if (eventIds.Any())
        {
            string inClause = BuildInClauseParams(eventIds.Count, 0);
            string updateQuery = $@"
                                    UPDATE published_events
                                          SET
                                              ""RetryCount"" = ""RetryCount"" + 1
                                          WHERE
                                              ""Id"" IN ({inClause});";
            await _dbContext.Database.ExecuteSqlRawAsync(
                updateQuery,
                eventIds.Cast<object>().ToArray());
        }
        return @events;
    }

    public async Task MarkPostPublishAttempt(List<PublishedEvent> events, CancellationToken cancellationToken = default)
    {
        List<Guid> ids = events.Select(e => e.Id).ToList();

        string inClause = BuildInClauseParams(ids.Count, 0);
        string updateSql = $@"
                    UPDATE published_events
                    SET ""Status"" = 2
                    WHERE ""Id"" IN ({inClause});";
        await _dbContext.Database.ExecuteSqlRawAsync(
            updateSql,
            ids.Cast<object>().ToArray());
    }

    public async Task SaveConsumedEvents(List<ReceivedEvent> receivedEvents, CancellationToken cancellationToken = default)
    {
        StringBuilder queryStringBuilder = new StringBuilder(@"
                        INSERT INTO received_events
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
                            VALUES");
        List<object> parameters = [];
        for (int i = 0; i < receivedEvents.Count; i++)
        {
            ReceivedEvent receivedEvent = receivedEvents[i];
            queryStringBuilder.Append($@"
                            (
                                {{{i * 12}}},
                                {{{i * 12 + 1}}},
                                {{{i * 12 + 2}}},
                                {{{i * 12 + 3}}},
                                {{{i * 12 + 4}}},
                                {{{i * 12 + 5}}},
                                {{{i * 12 + 6}}},
                                {{{i * 12 + 7}}},
                                {{{i * 12 + 8}}},
                                {{{i * 12 + 9}}},
                                {{{i * 12 + 10}}},
                                {{{i * 12 + 11}}}
                            )");
            if (i != receivedEvents.Count - 1)
            {
                queryStringBuilder.Append(',');
            }
            parameters.Add(receivedEvent.Id);
            parameters.Add(receivedEvent.Group);
            parameters.Add(receivedEvent.Topic);
            parameters.Add(receivedEvent.Content);
            parameters.Add(receivedEvent.RetryCount);
            parameters.Add(receivedEvent.SentAt);
            parameters.Add(receivedEvent.Timestamp);
            parameters.Add(receivedEvent.Status);
            parameters.Add(receivedEvent.ErrorMessage);
            parameters.Add(receivedEvent.UpdateTimestamp!);
            parameters.Add(receivedEvent.EventKey);
            parameters.Add(receivedEvent.Partition);
        }
        queryStringBuilder.Append(@" ON CONFLICT (""Id"") DO NOTHING;");

        await _dbContext.Database.ExecuteSqlRawAsync(
            queryStringBuilder.ToString(),
            parameters,
            cancellationToken);
    }

    public async Task<List<PublishedEvent>> GetProducedEventsByStatusAndElapsedTimespan(
        EventStatus eventStatus,
        TimeSpan timeSpan,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset cutOffTime = _timeService.GetUtcNow().Subtract(timeSpan);
        const string query = @"
                            SELECT
                                p.""Id"",
                                p.""Content"",
                                p.""EventKey"",
                                p.""RetryCount"",
                                p.""SentAt"",
                                p.""Status"",
                                p.""Timestamp"",
                                p.""Topic""
                            FROM published_events AS p
                            WHERE
                                p.""Status"" = {0} AND
                                p.""SentAt"" <= {1};";

        return await _dbContext.Database
            .SqlQueryRaw<PublishedEvent>(
                sql: query,
                eventStatus,
                cutOffTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReceivedEvent>> GetReceivedEventsByStatusAndElapsedTimespan(
        EventStatus eventStatus,
        TimeSpan timeSpan,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset cutOffTime = _timeService.GetUtcNow().Subtract(timeSpan);
        const string query = @"
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
                                FROM received_events AS r
                                  WHERE
                                      r.""Status"" = {0} AND
                                      r.""UpdateTimestamp"" <= {1};";
        return await _dbContext.Database
            .SqlQueryRaw<ReceivedEvent>(
                sql: query,
                eventStatus,
                cutOffTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteProducedEventsById(List<Guid> eventIds, CancellationToken cancellationToken = default)
    {
        string inClause = BuildInClauseParams(eventIds.Count, 0);
        string deleteQuery = $@"
                                    DELETE
                                        FROM published_events
                                      WHERE
                                          ""Id"" IN ({inClause});";
        await _dbContext.Database.ExecuteSqlRawAsync(deleteQuery, eventIds.Cast<object>().ToArray());
    }

    public async Task DeleteReceivedEventsById(
        List<Guid> eventIds,
        CancellationToken cancellationToken = default)
    {
        string inClause = BuildInClauseParams(eventIds.Count, 0);
        string deleteQuery = $@"
                                    DELETE
                                        FROM received_events
                                      WHERE
                                          ""Id"" IN ({inClause});";
        await _dbContext.Database.ExecuteSqlRawAsync(deleteQuery, eventIds.Cast<object>().ToArray());
    }

    private static string BuildInClauseParams(int count, int startIndex)
    {
        return string.Join(", ", Enumerable.Range(startIndex, count).Select(i => $"{{{i}}}"));
    }
}

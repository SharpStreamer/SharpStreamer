using System.Data;
using Dapper;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DotNetCore.SharpStreamer.EfCore.Npgsql;

public class EventsRepository<TDbContext>(
    TDbContext dbContext,
    ILogger<EventsRepository<TDbContext>> logger,
    ITimeService timeService) : IEventsRepository
    where TDbContext : DbContext
{
    public async Task<List<ReceivedEvent>> GetAndMarkEventsForProcessing(CancellationToken cancellationToken = default)
    {
        DateTimeOffset currentTime = timeService.GetUtcNow();
        List<ReceivedEvent> @events = await dbContext.Set<ReceivedEvent>()
            .Where(r => r.Status == EventStatus.Failed || r.Status == EventStatus.None)
            .Where(r => r.ExpiresAt > currentTime)
            .Where(r => r.RetryCount < 50)
            .OrderBy(r => r.Id)
            .Take(100)
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

    private static string CalculateErrorMessageValue(ReceivedEvent receivedEvent)
    {
        if (receivedEvent.ErrorMessage is null)
        {
            return "NULL";
        }

        return $"'{receivedEvent.ErrorMessage}'";
    }
}
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.SharpStreamer.EfCore.Npgsql;

public class EventsRepository<TDbContext>(
    TDbContext dbContext,
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
        Dictionary<Guid, ReceivedEvent> eventsData = @receivedEvents.ToDictionary(r => r.Id, r => r);
        await dbContext.Set<ReceivedEvent>()
            .Where(r => eventsData.ContainsKey(r.Id))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, r => eventsData[r.Id].Status), // TODO: Fix this
                cancellationToken);
    }
}
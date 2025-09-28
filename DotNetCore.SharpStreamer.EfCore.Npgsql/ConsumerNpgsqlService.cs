using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.SharpStreamer.EfCore.Npgsql;

public class ConsumerNpgsqlService<TDbContext>(TDbContext dbContext) : IConsumerService
    where TDbContext : DbContext
{
    public async Task SaveConsumedEvent(ReceivedEvent receivedEvent)
    {
        dbContext.Set<ReceivedEvent>().Add(receivedEvent);
        await dbContext.SaveChangesAsync();

        // detach the entity so it's no longer tracked
        dbContext.Entry(receivedEvent).State = EntityState.Detached;
    }
}
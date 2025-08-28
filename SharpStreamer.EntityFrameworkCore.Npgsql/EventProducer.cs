using Microsoft.EntityFrameworkCore;
using SharpStreamer.Abstractions;

namespace SharpStreamer.EntityFrameworkCore.Npgsql;

public class EventProducer<TDbContext>(TDbContext dbContext) : IEventProducer
    where TDbContext : DbContext
{
    public Task Produce<T>(T data) where T : IEvent
    {
        throw new NotImplementedException();
    }

    public Task ProduceDelayed<T>(T data, TimeSpan produceAfter) where T : IEvent
    {
        throw new NotImplementedException();
    }
}
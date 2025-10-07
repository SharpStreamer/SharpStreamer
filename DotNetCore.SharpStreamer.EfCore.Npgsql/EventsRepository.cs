using DotNetCore.SharpStreamer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.SharpStreamer.EfCore.Npgsql;

public class EventsRepository<TDbContext> : IEventsRepository
    where TDbContext : DbContext
{
    
}
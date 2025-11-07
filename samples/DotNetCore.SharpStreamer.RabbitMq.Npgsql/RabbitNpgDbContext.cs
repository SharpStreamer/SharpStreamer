using System.Reflection;
using DotNetCore.SharpStreamer.Storage.Npgsql;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql;

public class RabbitNpgDbContext(DbContextOptions<RabbitNpgDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
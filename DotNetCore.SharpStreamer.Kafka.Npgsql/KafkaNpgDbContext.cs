using System.Reflection;
using DotNetCore.SharpStreamer.Storage.Npgsql;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.SharpStreamer.Kafka.Npgsql;

public class KafkaNpgDbContext(DbContextOptions<KafkaNpgDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
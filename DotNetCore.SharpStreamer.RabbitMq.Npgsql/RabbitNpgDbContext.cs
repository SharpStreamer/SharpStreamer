using System.Reflection;
using DotNetCore.SharpStreamer.EfCore.Npgsql;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql;

public class RabbitNpgDbContext(DbContextOptions<RabbitNpgDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.ConfigureSharpStreamerNpgsql();
        base.OnModelCreating(modelBuilder);
    }
}
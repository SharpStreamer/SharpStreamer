using System.Reflection;
using DotNetCore.SharpStreamer.Storage.Npgsql;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.SharpStreamer.Npgsql.Npgsql;

public class NpgDbContext(DbContextOptions<NpgDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.ConfigureSharpStreamerNpgsql();
        base.OnModelCreating(modelBuilder);
    }
}
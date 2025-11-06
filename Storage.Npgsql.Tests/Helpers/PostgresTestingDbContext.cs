using System.Reflection;
using DotNetCore.SharpStreamer.Storage.Npgsql;
using Microsoft.EntityFrameworkCore;

namespace Storage.Npgsql.Tests.Helpers;

public class PostgresTestingDbContext(DbContextOptions<PostgresTestingDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.ConfigureSharpStreamerNpgsql();

        base.OnModelCreating(modelBuilder);
    }
}
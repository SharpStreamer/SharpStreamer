using System.Reflection;
using DotNetCore.SharpStreamer.Storage.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Storage.Sqlite.Tests.Helpers;

public class SqliteTestingDbContext(DbContextOptions<SqliteTestingDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.ConfigureSharpStreamerSqlite();

        base.OnModelCreating(modelBuilder);
    }
}

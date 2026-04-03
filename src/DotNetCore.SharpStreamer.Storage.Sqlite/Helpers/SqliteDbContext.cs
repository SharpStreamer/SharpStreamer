using Microsoft.EntityFrameworkCore;

namespace DotNetCore.SharpStreamer.Storage.Sqlite.Helpers;

public class SqliteDbContext(DbContextOptions<SqliteDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureSharpStreamerSqlite();
        base.OnModelCreating(modelBuilder);
    }
}

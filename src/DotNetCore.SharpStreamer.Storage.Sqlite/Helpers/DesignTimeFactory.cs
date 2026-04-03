using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotNetCore.SharpStreamer.Storage.Sqlite.Helpers;

public class DesignTimeFactory : IDesignTimeDbContextFactory<SqliteDbContext>
{
    public SqliteDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<SqliteDbContext> options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite("Just connection string")
            .Options;

        return new SqliteDbContext(options);
    }
}
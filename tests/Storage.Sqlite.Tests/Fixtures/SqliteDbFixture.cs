using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Storage.Sqlite.Tests.Helpers;

#nullable disable

namespace Storage.Sqlite.Tests.Fixtures;

public class SqliteDbFixture : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"sharp_streamer_test_{Guid.NewGuid():N}.db");

    public Func<DbContext> DbContextFactory { get; set; } = null;

    public async Task InitializeAsync()
    {
        DbContextOptions<SqliteTestingDbContext> options = new DbContextOptionsBuilder<SqliteTestingDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        DbContextFactory = () => new SqliteTestingDbContext(options);

        await using DbContext context = DbContextFactory();
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        return Task.CompletedTask;
    }
}

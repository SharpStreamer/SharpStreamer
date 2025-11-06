using Microsoft.EntityFrameworkCore;
using Storage.Npgsql.Tests.Helpers;
using Testcontainers.PostgreSql;

#nullable disable

namespace Storage.Npgsql.Tests.Fixtures;

public class PostgresDbFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithPassword("postgres")
        .WithUsername("postgres")
        .WithDatabase("sharp_streamer_db")
        .Build();

    public DbContext DbContext { get; set; } = null;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        DbContextOptions<PostgresTestingDbContext> options = new DbContextOptionsBuilder<PostgresTestingDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        DbContext = new PostgresTestingDbContext(options);
        await EnsureDatabaseCreatedAsync();
    }

    private async Task EnsureDatabaseCreatedAsync()
    {
        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}
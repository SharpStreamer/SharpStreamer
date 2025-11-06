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
        //.WithPortBinding(56490, 5432) // TODO: Delete this
        .WithDatabase("sharp_streamer_db")
        .Build();

    public Func<DbContext> DbContextFactory { get; set; } = null;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        DbContextOptions<PostgresTestingDbContext> options = new DbContextOptionsBuilder<PostgresTestingDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        DbContextFactory = () => new PostgresTestingDbContext(options);

        await using DbContext context = DbContextFactory();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }
}
using DotNetCore.SharpStreamer.Entities;
using Microsoft.EntityFrameworkCore;
using Storage.Npgsql.Tests.Fixtures;

namespace Storage.Npgsql.Tests.Helpers;

public class DatabaseTest(PostgresDbFixture postgresDbFixture) : IClassFixture<PostgresDbFixture>, IAsyncLifetime
{
    protected readonly DbContext DbContext = postgresDbFixture.DbContextFactory();

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await DbContext.Set<ReceivedEvent>().ExecuteDeleteAsync();
        await DbContext.Set<PublishedEvent>().ExecuteDeleteAsync();
        await DbContext.DisposeAsync();
    }
}
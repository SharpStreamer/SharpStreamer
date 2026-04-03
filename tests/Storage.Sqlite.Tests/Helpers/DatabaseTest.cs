using DotNetCore.SharpStreamer.Entities;
using Microsoft.EntityFrameworkCore;
using Storage.Sqlite.Tests.Fixtures;

namespace Storage.Sqlite.Tests.Helpers;

public class DatabaseTest(SqliteDbFixture sqliteDbFixture) : IClassFixture<SqliteDbFixture>, IAsyncLifetime
{
    protected readonly DbContext DbContext = sqliteDbFixture.DbContextFactory();

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

using AutoFixture;
using DotNetCore.SharpStreamer.Entities;
using Microsoft.EntityFrameworkCore;
using Storage.Npgsql.Tests.Fixtures;

namespace Storage.Npgsql.Tests;

[Collection(nameof(GlobalCollection))]
public class EventsRepositoryTests : IClassFixture<PostgresDbFixture>
{
    private readonly DbContext _dbContext;
    private readonly Fixture _fixture;
    public EventsRepositoryTests(
        PostgresDbFixture postgresDbFixture,
        DataFixtureConfig dataFixtureConfig)
    {
        _dbContext = postgresDbFixture.DbContext;
        _fixture = dataFixtureConfig.Fixture;
    }

    [Fact]
    public async Task Test1()
    {
        PublishedEvent x = _fixture.Create<PublishedEvent>();
        x.Content = @"{""opana"" : 12}";
        _dbContext.Set<PublishedEvent>().Add(x);
        await _dbContext.SaveChangesAsync();
        Assert.True(true);
    }

    [Fact]
    public void Test2()
    {
        Assert.True(true);
    }
}
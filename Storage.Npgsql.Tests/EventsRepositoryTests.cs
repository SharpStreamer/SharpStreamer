using System.Data;
using AutoFixture;
using Dapper;
using DotNetCore.SharpStreamer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Storage.Npgsql.Tests.Fixtures;

namespace Storage.Npgsql.Tests;

public class EventsRepositoryTests : IClassFixture<PostgresDbFixture>
{
    private readonly DbContext _dbContext;
    private readonly Fixture _fixture;
    public EventsRepositoryTests(PostgresDbFixture postgresDbFixture)
    {
        _dbContext = postgresDbFixture.DbContext;
        _fixture = new Fixture();
        _fixture.Customize<DateTimeOffset>(composer => 
            composer.FromFactory<DateTime>(
                datetime => new DateTimeOffset(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, TimeSpan.Zero)));
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
using Storage.Npgsql.Tests.Fixtures;

namespace Storage.Npgsql.Tests;

[Collection(nameof(GlobalCollection))]
public class SharpStreamerBusNpgsqlTests(PostgresDbFixture postgresDbFixture) : IClassFixture<PostgresDbFixture>
{
    [Fact]
    public void Test1()
    {
        Assert.True(true);
    }

    [Fact]
    public void Test2()
    {
        Assert.True(true);
    }
}
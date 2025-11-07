using Storage.Npgsql.Tests.Fixtures;

namespace Storage.Npgsql.Tests;

[Collection(nameof(GlobalCollection))]
public class SharpStreamerBusNpgsqlTests(PostgresDbFixture postgresDbFixture) : IClassFixture<PostgresDbFixture>
{
    
}
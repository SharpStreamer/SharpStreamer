using AutoFixture;

namespace Storage.Npgsql.Tests.Fixtures;

[CollectionDefinition(nameof(GlobalCollection))]
public class GlobalCollection : ICollectionFixture<DataFixtureConfig>;

public class DataFixtureConfig
{
    public Fixture Fixture { get; init; }

    public DataFixtureConfig()
    {
        Fixture = new Fixture();
        Fixture.Customize<DateTimeOffset>(composer => 
            composer.FromFactory<DateTime>(
                datetime => new DateTimeOffset(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, TimeSpan.Zero)));
    }
}
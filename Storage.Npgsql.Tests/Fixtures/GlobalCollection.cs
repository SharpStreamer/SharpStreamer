using AutoFixture;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;

namespace Storage.Npgsql.Tests.Fixtures;

[CollectionDefinition(nameof(GlobalCollection))]
public class GlobalCollection : ICollectionFixture<DataFixtureConfig>;

public class DataFixtureConfig
{
    private readonly Fixture _rawFixture = new Fixture();
    public Fixture Fixture { get; init; }

    public DataFixtureConfig()
    {
        Fixture = new Fixture();
        Fixture.Customize<DateTimeOffset>(composer => 
            composer.FromFactory<DateTime>(
                datetime => new DateTimeOffset(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, TimeSpan.Zero)));
        _rawFixture.Customize<DateTimeOffset>(composer => 
            composer.FromFactory<DateTime>(
                datetime => new DateTimeOffset(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, TimeSpan.Zero)));
        Fixture.Register(() =>
        {
            ReceivedEvent receivedEvent = _rawFixture.Create<ReceivedEvent>();
            receivedEvent.Content = @"{""data"" : null}";
            return receivedEvent;
        });
        Fixture.Register(() =>
        {
            PublishedEvent publishedEvent = _rawFixture.Create<PublishedEvent>();
            publishedEvent.Content = @"{""data"" : null}";
            return publishedEvent;
        });
    }
}
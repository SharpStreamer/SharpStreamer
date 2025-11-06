using AutoFixture;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Storage.Npgsql;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Storage.Npgsql.Tests.Fixtures;
using Storage.Npgsql.Tests.Helpers;

namespace Storage.Npgsql.Tests;

[Collection(nameof(GlobalCollection))]
public class EventsRepositoryTests : IClassFixture<PostgresDbFixture>
{
    private readonly DbContext _dbContext;
    private readonly Fixture _fixture;
    EventsRepository<PostgresTestingDbContext> _eventsRepository;
    private readonly ILogger<EventsRepository<PostgresTestingDbContext>> logger;
    private readonly IOptions<SharpStreamerOptions> sharpStreamerOptions;
    private readonly ITimeService timeService;
    public EventsRepositoryTests(
        PostgresDbFixture postgresDbFixture,
        DataFixtureConfig dataFixtureConfig)
    {
        _dbContext = postgresDbFixture.DbContext;
        _fixture = dataFixtureConfig.Fixture;
        logger = Substitute.For<ILogger<EventsRepository<PostgresTestingDbContext>>>();
        sharpStreamerOptions = Substitute.For<IOptions<SharpStreamerOptions>>();
        timeService = Substitute.For<ITimeService>();
        _eventsRepository = new EventsRepository<PostgresTestingDbContext>(
            (PostgresTestingDbContext)postgresDbFixture.DbContext,
            logger,
            sharpStreamerOptions,
            timeService);
    }

    [Fact]
    public async Task GetAndMarkEventsForProcessing_ReturnsCorrectEventsForProcessing()
    {
        // Arrange
        DateTimeOffset currentTime = _fixture.Create<DateTimeOffset>();
        timeService.GetUtcNow().Returns(currentTime);
        sharpStreamerOptions.Value.Returns(new SharpStreamerOptions()
        {
            ProcessingBatchSize = 10
        });
        List<ReceivedEvent> receivedEvents = InitializeReceivedEventsForProcessingTest(currentTime);
        List<ReceivedEvent> expectedEventsToBeReturned =
        [
            receivedEvents[0],
            receivedEvents[5],
            receivedEvents[6],
            receivedEvents[7],
            receivedEvents[8],
            receivedEvents[9],
        ];
        _dbContext.Set<ReceivedEvent>().AddRange(receivedEvents);
        await _dbContext.SaveChangesAsync();

        // Assert
        List<ReceivedEvent> returnedEvents = await _eventsRepository.GetAndMarkEventsForProcessing(CancellationToken.None);

        // Act
        returnedEvents.Should().BeEquivalentTo(expectedEventsToBeReturned);

        // Clear database
        await _dbContext.Set<ReceivedEvent>().ExecuteDeleteAsync();
    }

    [Fact]
    public async Task GetAndMarkEventsForProcessing_WhenBatchSizeIsLittle_ReturnsCorrectEventsForProcessing()
    {
        // Arrange
        DateTimeOffset currentTime = _fixture.Create<DateTimeOffset>();
        timeService.GetUtcNow().Returns(currentTime);
        sharpStreamerOptions.Value.Returns(new SharpStreamerOptions()
        {
            ProcessingBatchSize = 3
        });
        List<ReceivedEvent> receivedEvents = InitializeReceivedEventsForProcessingTest(currentTime);
        List<ReceivedEvent> expectedEventsToBeReturned =
        [
            receivedEvents[0],
            receivedEvents[5],
            receivedEvents[6],
            receivedEvents[7],
            receivedEvents[8],
            receivedEvents[9],
        ];
        expectedEventsToBeReturned = expectedEventsToBeReturned.OrderBy(e => e.Timestamp).Take(3).ToList();

        _dbContext.Set<ReceivedEvent>().AddRange(receivedEvents);
        await _dbContext.SaveChangesAsync();

        // Assert
        List<ReceivedEvent> returnedEvents = await _eventsRepository.GetAndMarkEventsForProcessing(CancellationToken.None);

        // Act
        returnedEvents.Should().BeEquivalentTo(expectedEventsToBeReturned);

        // Clear database
        await _dbContext.Set<ReceivedEvent>().ExecuteDeleteAsync();
    }

    [Fact]
    public async Task GetAndMarkEventsForProcessing_UpdatesOnlyReturnedObjects()
    {
        // Arrange
        DateTimeOffset currentTime = _fixture.Create<DateTimeOffset>();
        timeService.GetUtcNow().Returns(currentTime);
        sharpStreamerOptions.Value.Returns(new SharpStreamerOptions()
        {
            ProcessingBatchSize = 3
        });
        List<ReceivedEvent> receivedEvents = InitializeReceivedEventsForProcessingTest(currentTime);
        List<ReceivedEvent> expectedEventsToBeReturned =
        [
            receivedEvents[0],
            receivedEvents[5],
            receivedEvents[6],
            receivedEvents[7],
            receivedEvents[8],
            receivedEvents[9],
        ];
        expectedEventsToBeReturned = expectedEventsToBeReturned.OrderBy(e => e.Timestamp).Take(3).ToList();

        _dbContext.Set<ReceivedEvent>().AddRange(receivedEvents);
        await _dbContext.SaveChangesAsync();

        // Assert
        List<ReceivedEvent> returnedEvents = await _eventsRepository.GetAndMarkEventsForProcessing(CancellationToken.None);

        // Act
        List<ReceivedEvent> dbEvents = await _dbContext.Set<ReceivedEvent>().ToListAsync();
        foreach (ReceivedEvent dbEvent in dbEvents)
        {
            if (expectedEventsToBeReturned.Any(e => e.Id == dbEvent.Id))
            {
                dbEvent.Status.Should().Be(EventStatus.InProgress);
            }
            else
            {
                dbEvent.Status.Should().Be(receivedEvents.Single(e => e.Id == dbEvent.Id).Status);
            }
        }

        // Clear database
        await _dbContext.Set<ReceivedEvent>().ExecuteDeleteAsync();
    }

    /// <summary>
    ///     Generates events, last 5 of them should be processed and also first should be processed.
    ///     Other ones should not be processed
    /// </summary>
    private List<ReceivedEvent> InitializeReceivedEventsForProcessingTest(DateTimeOffset currentTime)
    {
        List<ReceivedEvent> receivedEvents = _fixture.Build<ReceivedEvent>()
            .With(r => r.Content, @"{""data"" : null}")
            .CreateMany<ReceivedEvent>(10)
            .ToList();
        for (int i = 0; i < 5; i++)
        {
            receivedEvents[i].Status = EventStatus.Failed;
            receivedEvents[i].RetryCount = 3;
        }
        for (int i = 0; i < 2; i++)
        {
            receivedEvents[i].UpdateTimestamp = currentTime.AddSeconds(-21);
        }
        for (int i = 2; i < 5; i++)
        {
            receivedEvents[i].UpdateTimestamp = currentTime.AddSeconds(-20);
        }
        for (int i = 5; i < 10; i++)
        {
            receivedEvents[i].Status = EventStatus.None;
            receivedEvents[i].UpdateTimestamp = null;
            receivedEvents[i].RetryCount = 0;
        }

        receivedEvents[1].RetryCount = 50;
        return receivedEvents;
    }
}
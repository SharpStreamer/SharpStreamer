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
public class EventsRepositoryTests : IClassFixture<PostgresDbFixture>, IAsyncLifetime
{
    private readonly DbContext _dbContext;
    private readonly Fixture _fixture;
    private readonly EventsRepository<PostgresTestingDbContext> _eventsRepository;
    private readonly ILogger<EventsRepository<PostgresTestingDbContext>> _logger;
    private readonly IOptions<SharpStreamerOptions> _sharpStreamerOptions;
    private readonly ITimeService _timeService;
    public EventsRepositoryTests(
        PostgresDbFixture postgresDbFixture,
        DataFixtureConfig dataFixtureConfig)
    {
        _dbContext = postgresDbFixture.DbContext;
        _fixture = dataFixtureConfig.Fixture;
        _logger = Substitute.For<ILogger<EventsRepository<PostgresTestingDbContext>>>();
        _sharpStreamerOptions = Substitute.For<IOptions<SharpStreamerOptions>>();
        _timeService = Substitute.For<ITimeService>();
        _eventsRepository = new EventsRepository<PostgresTestingDbContext>(
            (PostgresTestingDbContext)postgresDbFixture.DbContext,
            _logger,
            _sharpStreamerOptions,
            _timeService);
    }

    [Fact]
    public async Task GetAndMarkEventsForProcessing_ReturnsCorrectEventsForProcessing()
    {
        // Arrange
        DateTimeOffset currentTime = _fixture.Create<DateTimeOffset>();
        _timeService.GetUtcNow().Returns(currentTime);
        _sharpStreamerOptions.Value.Returns(new SharpStreamerOptions()
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
    }

    [Fact]
    public async Task GetAndMarkEventsForProcessing_WhenBatchSizeIsLittle_ReturnsCorrectEventsForProcessing()
    {
        // Arrange
        DateTimeOffset currentTime = _fixture.Create<DateTimeOffset>();
        _timeService.GetUtcNow().Returns(currentTime);
        _sharpStreamerOptions.Value.Returns(new SharpStreamerOptions()
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
    }

    [Fact]
    public async Task GetAndMarkEventsForProcessing_UpdatesOnlyReturnedObjects()
    {
        // Arrange
        DateTimeOffset currentTime = _fixture.Create<DateTimeOffset>();
        _timeService.GetUtcNow().Returns(currentTime);
        _sharpStreamerOptions.Value.Returns(new SharpStreamerOptions()
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
        await _eventsRepository.GetAndMarkEventsForProcessing(CancellationToken.None);

        // Act
        _dbContext.ChangeTracker.Clear(); // To retrieve fresh data.
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
    }

    [Fact]
    public async Task MarkPostProcessing_MarksPassedEvents()
    {
        // Arrange
        DateTimeOffset currentTime = _fixture.Create<DateTimeOffset>();
        _timeService.GetUtcNow().Returns(currentTime);
        List<ReceivedEvent> receivedEvents = _fixture.CreateMany<ReceivedEvent>(3).ToList();
        _dbContext.Set<ReceivedEvent>().AddRange(receivedEvents);
        await _dbContext.SaveChangesAsync();

        receivedEvents[0].Status = EventStatus.Failed;
        receivedEvents[0].ErrorMessage = "Error message 1";

        receivedEvents[1].Status = EventStatus.Succeeded;
        receivedEvents[1].ErrorMessage = null;

        receivedEvents[2].Status = EventStatus.Failed;
        receivedEvents[2].ErrorMessage = null;
    
        // Assert
        await _eventsRepository.MarkPostProcessing(receivedEvents);

        // Act
        _dbContext.ChangeTracker.Clear(); // To retrieve fresh data.
        List<ReceivedEvent> dbEvents = await _dbContext.Set<ReceivedEvent>().ToListAsync();

        dbEvents.Single(r => r.Id == receivedEvents[0].Id).Status.Should().Be(EventStatus.Failed);
        dbEvents.Single(r => r.Id == receivedEvents[0].Id).ErrorMessage.Should().Be("Error message 1");

        dbEvents.Single(r => r.Id == receivedEvents[1].Id).Status.Should().Be(EventStatus.Succeeded);
        dbEvents.Single(r => r.Id == receivedEvents[1].Id).ErrorMessage.Should().Be(null);

        dbEvents.Single(r => r.Id == receivedEvents[2].Id).Status.Should().Be(EventStatus.Failed);
        dbEvents.Single(r => r.Id == receivedEvents[2].Id).ErrorMessage.Should().Be(null);
    }

    /// <summary>
    ///     Generates events, last 5 of them should be processed and also first should be processed.
    ///     Other ones should not be processed
    /// </summary>
    private List<ReceivedEvent> InitializeReceivedEventsForProcessingTest(DateTimeOffset currentTime)
    {
        List<ReceivedEvent> receivedEvents = _fixture.CreateMany<ReceivedEvent>(10).ToList();
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

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Set<ReceivedEvent>().ExecuteDeleteAsync();
        await _dbContext.Set<PublishedEvent>().ExecuteDeleteAsync();
        _dbContext.ChangeTracker.Clear();
    }
}
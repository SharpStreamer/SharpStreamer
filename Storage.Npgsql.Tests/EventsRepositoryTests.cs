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
    private readonly Func<DbContext> _dbContextFactory;
    public EventsRepositoryTests(
        PostgresDbFixture postgresDbFixture,
        DataFixtureConfig dataFixtureConfig)
    {
        _dbContext = postgresDbFixture.DbContextFactory();
        _dbContextFactory = postgresDbFixture.DbContextFactory;
        _fixture = dataFixtureConfig.Fixture;
        _logger = Substitute.For<ILogger<EventsRepository<PostgresTestingDbContext>>>();
        _sharpStreamerOptions = Substitute.For<IOptions<SharpStreamerOptions>>();
        _timeService = Substitute.For<ITimeService>();
        _eventsRepository = new EventsRepository<PostgresTestingDbContext>(
            (PostgresTestingDbContext)_dbContext,
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

        // Act
        List<ReceivedEvent> returnedEvents = await _eventsRepository.GetAndMarkEventsForProcessing(CancellationToken.None);

        // Assert
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

        // Act
        List<ReceivedEvent> returnedEvents = await _eventsRepository.GetAndMarkEventsForProcessing(CancellationToken.None);

        // Assert
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
        foreach (ReceivedEvent expectedEvent in expectedEventsToBeReturned)
        {
            expectedEvent.RetryCount = 1;
        }

        _dbContext.Set<ReceivedEvent>().AddRange(receivedEvents);
        await _dbContext.SaveChangesAsync();

        // Act
        await _eventsRepository.GetAndMarkEventsForProcessing(CancellationToken.None);

        // Assert
        await using DbContext assertDbContext = _dbContextFactory();
        List<ReceivedEvent> dbEvents = await assertDbContext.Set<ReceivedEvent>().ToListAsync();
        foreach (ReceivedEvent dbEvent in dbEvents)
        {
            if (expectedEventsToBeReturned.Any(e => e.Id == dbEvent.Id))
            {
                dbEvent.Status.Should().Be(EventStatus.InProgress);
                dbEvent.RetryCount.Should().Be(2);
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
    
        // Act
        await _eventsRepository.MarkPostProcessing(receivedEvents);

        // Assert
        await using DbContext assertDbContext = _dbContextFactory();
        List<ReceivedEvent> dbEvents = await assertDbContext.Set<ReceivedEvent>().ToListAsync();

        dbEvents.Single(r => r.Id == receivedEvents[0].Id).Status.Should().Be(EventStatus.Failed);
        dbEvents.Single(r => r.Id == receivedEvents[0].Id).ErrorMessage.Should().Be("Error message 1");
        dbEvents.Single(r => r.Id == receivedEvents[0].Id).UpdateTimestamp.Should().Be(currentTime);

        dbEvents.Single(r => r.Id == receivedEvents[1].Id).Status.Should().Be(EventStatus.Succeeded);
        dbEvents.Single(r => r.Id == receivedEvents[1].Id).ErrorMessage.Should().Be(null);
        dbEvents.Single(r => r.Id == receivedEvents[1].Id).UpdateTimestamp.Should().Be(currentTime);

        dbEvents.Single(r => r.Id == receivedEvents[2].Id).Status.Should().Be(EventStatus.Failed);
        dbEvents.Single(r => r.Id == receivedEvents[2].Id).ErrorMessage.Should().Be(null);
        dbEvents.Single(r => r.Id == receivedEvents[2].Id).UpdateTimestamp.Should().Be(currentTime);
    }

    // ... Existing code up to MarkPostProcessing_MarksPassedEvents ...

    [Fact]
    public async Task GetPredecessorIds_ReturnsCorrectIds()
    {
        // Arrange
        DateTimeOffset currentTime = _fixture.Create<DateTimeOffset>();
        string eventKey = _fixture.Create<string>();
        // Statuses to be considered predecessors
        List<EventStatus> predecessorStatuses = [EventStatus.Failed, EventStatus.None, EventStatus.InProgress];
        
        List<ReceivedEvent> allEvents = _fixture.CreateMany<ReceivedEvent>(4).ToList();
        
        // 1. Matching EventKey, Predecessor Status, and Timestamp < currentTime (Should be returned)
        allEvents[0].EventKey = eventKey;
        allEvents[0].Status = predecessorStatuses[_fixture.Create<int>() % predecessorStatuses.Count];
        allEvents[0].Timestamp = currentTime.AddMinutes(-5);

        // 2. Matching EventKey, Success Status, and Timestamp < currentTime (Should NOT be returned)
        allEvents[1].EventKey = eventKey;
        allEvents[1].Status = EventStatus.Succeeded;
        allEvents[1].Timestamp = currentTime.AddMinutes(-5);

        // 3. Matching EventKey, Predecessor Status, and Timestamp >= currentTime (Should NOT be returned)
        allEvents[2].EventKey = eventKey;
        allEvents[2].Status = predecessorStatuses[_fixture.Create<int>() % predecessorStatuses.Count];
        allEvents[2].Timestamp = currentTime.AddMinutes(5);

        // 4. Different EventKey, Predecessor Status, and Timestamp < currentTime (Should NOT be returned)
        allEvents[3].EventKey = _fixture.Create<string>();
        allEvents[3].Status = predecessorStatuses[_fixture.Create<int>() % predecessorStatuses.Count];
        allEvents[3].Timestamp = currentTime.AddMinutes(-5);

        List<Guid> expectedIds = new List<Guid> { allEvents[0].Id };

        _dbContext.Set<ReceivedEvent>().AddRange(allEvents);
        await _dbContext.SaveChangesAsync();

        // Act
        List<Guid> returnedIds = await _eventsRepository.GetPredecessorIds(eventKey, currentTime, CancellationToken.None);

        // Assert
        returnedIds.Should().BeEquivalentTo(expectedIds);
    }

    [Fact]
    public async Task GetEventsToPublish_ReturnsAndMarksCorrectEvents()
    {
        // Arrange
        DateTimeOffset currentTime = _fixture.Create<DateTimeOffset>();
        _timeService.GetUtcNow().Returns(currentTime);
        _sharpStreamerOptions.Value.Returns(new SharpStreamerOptions()
        {
            ProcessingBatchSize = 2
        });
        
        List<PublishedEvent> allEvents = _fixture.CreateMany<PublishedEvent>(5).ToList();
        foreach (PublishedEvent dbEvent in allEvents)
        {
            dbEvent.RetryCount = 0;
        }

        // 1. Status == None, SentAt < currentTime (Should be returned) - Limit 3
        allEvents[0].Status = EventStatus.None;
        allEvents[0].SentAt = currentTime.AddMinutes(-5);

        allEvents[1].Status = EventStatus.None;
        allEvents[1].SentAt = currentTime.AddMinutes(-1);

        allEvents[2].Status = EventStatus.None;
        allEvents[2].SentAt = currentTime.AddMinutes(-10); // Order by SentAt

        // 4. Status == None, SentAt >= currentTime (Should NOT be returned)
        allEvents[3].Status = EventStatus.None;
        allEvents[3].SentAt = currentTime.AddMinutes(1);

        // 5. Status != None, SentAt < currentTime (Should NOT be returned)
        allEvents[4].Status = EventStatus.Succeeded;
        allEvents[4].SentAt = currentTime.AddMinutes(-5);

        List<PublishedEvent> expectedEventsToBeReturned = 
            [allEvents[2], allEvents[0]]; // Ordered by SentAt and limited to 3

        _dbContext.Set<PublishedEvent>().AddRange(allEvents);
        await _dbContext.SaveChangesAsync();

        // Act
        List<PublishedEvent> returnedEvents = await _eventsRepository.GetEventsToPublish(CancellationToken.None);

        // Assert
        returnedEvents.Should().BeEquivalentTo(expectedEventsToBeReturned);

        // Check update (RetryCount should be incremented for returned events)
        await using DbContext assertDbContext = _dbContextFactory();
        foreach (PublishedEvent returnedEvent in returnedEvents)
        {
            (await assertDbContext.Set<PublishedEvent>().SingleAsync(e => e.Id == returnedEvent.Id)).RetryCount.Should().Be(1);
        }
        (await assertDbContext.Set<PublishedEvent>().SingleAsync(e => e.Id == allEvents[3].Id)).RetryCount.Should().Be(0);
        (await assertDbContext.Set<PublishedEvent>().SingleAsync(e => e.Id == allEvents[1].Id)).RetryCount.Should().Be(0);
        (await assertDbContext.Set<PublishedEvent>().SingleAsync(e => e.Id == allEvents[4].Id)).RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task MarkPostPublishAttempt_MarksEventsAsSucceeded()
    {
        // Arrange
        List<PublishedEvent> publishedEvents = _fixture.CreateMany<PublishedEvent>(3).ToList();
        publishedEvents.ForEach(e => e.Status = EventStatus.None); // Initial status
        
        _dbContext.Set<PublishedEvent>().AddRange(publishedEvents);
        await _dbContext.SaveChangesAsync();

        // Act
        await _eventsRepository.MarkPostPublishAttempt(publishedEvents);

        // Assert
        await using DbContext assertDbContext = _dbContextFactory();
        List<PublishedEvent> dbEvents = await assertDbContext.Set<PublishedEvent>().ToListAsync();
        
        // EventStatus.Succeeded is int value 2
        dbEvents.Should().AllSatisfy(e => e.Status.Should().Be(EventStatus.Succeeded));
        dbEvents.Should().AllSatisfy(e => publishedEvents.Select(pe => pe.Id).Contains(e.Id).Should().BeTrue());
    }

    [Fact]
    public async Task SaveConsumedEvents_InsertsNewEventsAndIgnoresConflict()
    {
        // Arrange
        List<ReceivedEvent> newEvents = _fixture.CreateMany<ReceivedEvent>(2).ToList();
        ReceivedEvent existingEvent = _fixture.Create<ReceivedEvent>();
        
        _dbContext.Set<ReceivedEvent>().Add(existingEvent);
        await _dbContext.SaveChangesAsync();

        // Create a list where one event has a conflicting ID
        newEvents[1].Id = existingEvent.Id;
        newEvents[1].Content = @"{""opana"" : 1}";
        List<ReceivedEvent> eventsToSave = new List<ReceivedEvent>
        {
            newEvents[0],
            newEvents[1] // Duplicate ID, different content
        };

        // Act
        await _eventsRepository.SaveConsumedEvents(eventsToSave);

        // Assert
        await using DbContext assertDbContext = _dbContextFactory();
        List<ReceivedEvent> dbEvents = await assertDbContext.Set<ReceivedEvent>().ToListAsync();

        // Total events should be 2: the one new one (index 0 (index 1 was duplicated with id)) and the original existing one.
        dbEvents.Should().HaveCount(2);

        // New events should be in the DB
        dbEvents.Should().Contain(e => e.Id == newEvents[0].Id);
        
        // The event with the conflicting ID should remain the original one (ON CONFLICT DO NOTHING)
        dbEvents.Single(e => e.Id == existingEvent.Id).Content.Should().Be(existingEvent.Content);

        // Check if the initial existing event's content hasn't been updated by the 'new' event with the same ID
        dbEvents.Single(e => e.Id == existingEvent.Id).Content.Should().NotBe(@"{""opana"" : 1}");
    }

    [Fact]
    public async Task GetProducedEventsByStatusAndElapsedTimespan_ReturnsCorrectEvents()
    {
        // Arrange
        DateTimeOffset currentTime = _fixture.Create<DateTimeOffset>();
        TimeSpan timeSpan = TimeSpan.FromHours(1); // 1 hour elapsed timespan
        _timeService.GetUtcNow().Returns(currentTime);
        DateTimeOffset cutOffTime = currentTime.Subtract(timeSpan);

        List<PublishedEvent> allEvents = _fixture.CreateMany<PublishedEvent>(5).ToList();

        // 1. Status == None, SentAt <= cutOffTime (Should be returned)
        allEvents[0].Status = EventStatus.None;
        allEvents[0].SentAt = cutOffTime.AddMinutes(-5); 

        // 2. Status == None, SentAt > cutOffTime (Should NOT be returned)
        allEvents[1].Status = EventStatus.None;
        allEvents[1].SentAt = cutOffTime.AddMinutes(5); 

        // 3. Status != None, SentAt <= cutOffTime (Should NOT be returned)
        allEvents[2].Status = EventStatus.Succeeded;
        allEvents[2].SentAt = cutOffTime.AddMinutes(-5);

        // 4. Status == None, SentAt == cutOffTime (Should be returned)
        allEvents[3].Status = EventStatus.None;
        allEvents[3].SentAt = cutOffTime;

        List<Guid> expectedIds = [ allEvents[0].Id, allEvents[3].Id ];
        
        _dbContext.Set<PublishedEvent>().AddRange(allEvents);
        await _dbContext.SaveChangesAsync();

        // Act
        _dbContext.ChangeTracker.Clear();
        List<PublishedEvent> returnedEvents = await _eventsRepository.GetProducedEventsByStatusAndElapsedTimespan(
            EventStatus.None, timeSpan, CancellationToken.None);

        // Assert
        returnedEvents.Select(e => e.Id).Should().BeEquivalentTo(expectedIds);
    }

    [Fact]
    public async Task GetReceivedEventsByStatusAndElapsedTimespan_ReturnsCorrectEvents()
    {
        // Arrange
        DateTimeOffset currentTime = _fixture.Create<DateTimeOffset>();
        TimeSpan timeSpan = TimeSpan.FromMinutes(30); // 30 minutes elapsed timespan
        _timeService.GetUtcNow().Returns(currentTime);
        DateTimeOffset cutOffTime = currentTime.Subtract(timeSpan);

        List<ReceivedEvent> allEvents = _fixture.CreateMany<ReceivedEvent>(5).ToList();

        // 1. Status == Failed, UpdateTimestamp <= cutOffTime (Should be returned)
        allEvents[0].Status = EventStatus.Failed;
        allEvents[0].UpdateTimestamp = cutOffTime.AddMinutes(-5); 

        // 2. Status == Failed, UpdateTimestamp > cutOffTime (Should NOT be returned)
        allEvents[1].Status = EventStatus.Failed;
        allEvents[1].UpdateTimestamp = cutOffTime.AddMinutes(5); 

        // 3. Status != Failed, UpdateTimestamp <= cutOffTime (Should NOT be returned)
        allEvents[2].Status = EventStatus.Succeeded;
        allEvents[2].UpdateTimestamp = cutOffTime.AddMinutes(-5);

        // 4. Status == Failed, UpdateTimestamp == cutOffTime (Should be returned)
        allEvents[3].Status = EventStatus.Failed;
        allEvents[3].UpdateTimestamp = cutOffTime;

        // 5. Status == Failed, UpdateTimestamp is null (Should NOT be returned, as it's not <= cutOffTime)
        allEvents[4].Status = EventStatus.Failed;
        allEvents[4].UpdateTimestamp = null; 

        List<Guid> expectedIds = [ allEvents[0].Id, allEvents[3].Id ];
        
        _dbContext.Set<ReceivedEvent>().AddRange(allEvents);
        await _dbContext.SaveChangesAsync();

        // Act
        _dbContext.ChangeTracker.Clear();
        List<ReceivedEvent> returnedEvents = await _eventsRepository.GetReceivedEventsByStatusAndElapsedTimespan(
            EventStatus.Failed, timeSpan, CancellationToken.None);

        // Assert
        returnedEvents.Select(e => e.Id).Should().BeEquivalentTo(expectedIds);
    }

    [Fact]
    public async Task DeleteProducedEventsById_DeletesCorrectEvents()
    {
        // Arrange
        List<PublishedEvent> allEvents = _fixture.CreateMany<PublishedEvent>(3).ToList();
        List<Guid> idsToDelete = [ allEvents[0].Id, allEvents[2].Id ];

        _dbContext.Set<PublishedEvent>().AddRange(allEvents);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        
        (await _dbContext.Set<PublishedEvent>().CountAsync()).Should().Be(3);

        // Act
        await _eventsRepository.DeleteProducedEventsById(idsToDelete, CancellationToken.None);

        // Assert
        await using DbContext assertDbContext = _dbContextFactory();
        List<PublishedEvent> remainingEvents = await assertDbContext.Set<PublishedEvent>().ToListAsync();
        
        remainingEvents.Should().HaveCount(1);
        remainingEvents.Should().ContainSingle(e => e.Id == allEvents[1].Id);
    }
    
    [Fact]
    public async Task DeleteReceivedEventsById_DeletesCorrectEvents()
    {
        // Arrange
        List<ReceivedEvent> allEvents = _fixture.CreateMany<ReceivedEvent>(3).ToList();
        List<Guid> idsToDelete = [ allEvents[0].Id, allEvents[2].Id ];

        _dbContext.Set<ReceivedEvent>().AddRange(allEvents);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        
        (await _dbContext.Set<ReceivedEvent>().CountAsync()).Should().Be(3);

        // Act
        await _eventsRepository.DeleteReceivedEventsById(idsToDelete, CancellationToken.None);

        // Assert
        await using DbContext assertDbContext = _dbContextFactory();
        List<ReceivedEvent> remainingEvents = await assertDbContext.Set<ReceivedEvent>().ToListAsync();
        
        remainingEvents.Should().HaveCount(1);
        remainingEvents.Should().ContainSingle(e => e.Id == allEvents[1].Id);
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
        await _dbContext.DisposeAsync();
    }
}
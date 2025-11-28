using AutoFixture;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Storage.Npgsql;
using DotNetCore.SharpStreamer.Storage.Npgsql.Abstractions;
using FluentAssertions;
using Medallion.Threading;
using Microsoft.Extensions.Options;
using NSubstitute;
using Storage.Npgsql.Tests.Fixtures;

namespace Storage.Npgsql.Tests;

[Collection(nameof(GlobalCollection))]
public class EventsProcessorServiceTests
{
    private readonly string _consumerGroup = Guid.NewGuid().ToString();
    private readonly EventsProcessorService _service;
    private readonly IEventsRepository _eventsRepository;
    private readonly IEventProcessor _eventProcessor;
    private readonly IOptions<SharpStreamerOptions> _options;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly Fixture _fixture;
    private readonly IDistributedSynchronizationHandle _lockHandle;
    private readonly IDistributedLock _distributedLock;

    public EventsProcessorServiceTests(DataFixtureConfig fixtureConfig)
    {
        _fixture = fixtureConfig.Fixture;
        _eventsRepository = Substitute.For<IEventsRepository>();
        _eventProcessor = Substitute.For<IEventProcessor>();
        _options = Substitute.For<IOptions<SharpStreamerOptions>>();
        _lockProvider = Substitute.For<IDistributedLockProvider>();
        
        // Setup lock provider to return a disposable handle for successful acquisition
        _lockHandle = Substitute.For<IDistributedSynchronizationHandle>();
        _distributedLock = Substitute.For<IDistributedLock>();
        _distributedLock
            .AcquireAsync(
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .Returns(_lockHandle);
        _lockProvider.CreateLock(
            Arg.Any<string>())
            .Returns(_distributedLock);
        _lockHandle.DisposeAsync().Returns(ValueTask.CompletedTask);

        SharpStreamerOptions options = new()
        {
            ConsumerGroup = _consumerGroup,
        };
        _options.Value.Returns(options);
        _service = new(
            _eventsRepository,
            _eventProcessor,
            _options,
            _lockProvider);
    }

    [Fact]
    public async Task ProcessEvents_SuccessfulExecution_ProcessesEventsCorrectly()
    {
        // Arrange
        List<ReceivedEvent> events = _fixture.CreateMany<ReceivedEvent>(3).ToList();
        SetupSuccessfulEventProcessing(events);

        // Act
        await _service.ProcessEvents();

        // Assert
        await VerifyLockAcquisition();
        await VerifyEventsRetrieval();
        VerifyEventProcessing(events);
        await VerifyEventsMarkedForPostProcessing(events);
    }

    [Fact]
    public async Task ProcessEvents_LockAcquisitionFails_ThrowsException()
    {
        // Arrange - Return null from AcquireAsync to simulate lock failure
        _distributedLock
            .AcquireAsync(Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns((IDistributedSynchronizationHandle)null!);

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _service.ProcessEvents());
    }

    [Fact]
    public async Task ProcessEvents_NoEventsReturned_DoesNotProcessEvents()
    {
        // Arrange
        List<ReceivedEvent> emptyEvents = new();
        _eventsRepository
            .GetAndMarkEventsForProcessing(Arg.Any<CancellationToken>())
            .Returns(emptyEvents);

        // Act
        await _service.ProcessEvents();

        // Assert
        await VerifyLockAcquisition();
        await VerifyEventsRetrieval();
        await _eventProcessor
            .DidNotReceive()
            .ProcessEvent(Arg.Any<ReceivedEvent>(), Arg.Any<Dictionary<Guid, EventStatus>>());
        await _eventsRepository
            .DidNotReceive()
            .MarkPostProcessing(Arg.Any<ReceivedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessEvents_EventProcessorThrowsException_StopsProcessing()
    {
        // Arrange
        List<ReceivedEvent> events = _fixture.CreateMany<ReceivedEvent>(3).ToList();
        _eventsRepository
            .GetAndMarkEventsForProcessing(Arg.Any<CancellationToken>())
            .Returns(events);

        // First event throws exception - should stop processing
        _eventProcessor
            .ProcessEvent(Arg.Is(events[0]), Arg.Any<Dictionary<Guid, EventStatus>>())
            .Returns<(Guid, EventStatus, string?)>(_ => throw new InvalidOperationException("Processing failed"));

        // Act & Assert - Should throw and stop processing
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ProcessEvents());

        // Verify that processing was attempted only for the first event
        await _eventProcessor
            .Received(1)
            .ProcessEvent(Arg.Is(events[0]), Arg.Any<Dictionary<Guid, EventStatus>>());
        
        // Should not process the remaining events due to exception
        await _eventProcessor
            .DidNotReceive()
            .ProcessEvent(Arg.Is(events[1]), Arg.Any<Dictionary<Guid, EventStatus>>());
        await _eventProcessor
            .DidNotReceive()
            .ProcessEvent(Arg.Is(events[2]), Arg.Any<Dictionary<Guid, EventStatus>>());
    }

    [Fact]
    public async Task ProcessEvents_EventProcessorReturnsEmptyId_DoesNotMarkForPostProcessing()
    {
        // Arrange
        List<ReceivedEvent> events = _fixture.CreateMany<ReceivedEvent>(2).ToList();
        _eventsRepository
            .GetAndMarkEventsForProcessing(Arg.Any<CancellationToken>())
            .Returns(events);

        // First event returns empty ID, second returns valid ID
        _eventProcessor
            .ProcessEvent(Arg.Is(events[0]), Arg.Any<Dictionary<Guid, EventStatus>>())
            .Returns((Guid.Empty, EventStatus.Failed, "Error message"));
        
        _eventProcessor
            .ProcessEvent(Arg.Is(events[1]), Arg.Any<Dictionary<Guid, EventStatus>>())
            .Returns((events[1].Id, EventStatus.Succeeded, (string?)null));

        // Act
        await _service.ProcessEvents();

        // Assert
        await _eventsRepository
            .DidNotReceive()
            .MarkPostProcessing(Arg.Is(events[0]), Arg.Any<CancellationToken>());
        await _eventsRepository
            .Received(1)
            .MarkPostProcessing(Arg.Is(events[1]), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessEvents_ExceptionMessageTooLong_TruncatesErrorMessage()
    {
        // Arrange
        List<ReceivedEvent> events = _fixture.CreateMany<ReceivedEvent>(1).ToList();
        string longErrorMessage = new('A', 1500); // Longer than 1000 characters
        
        _eventsRepository
            .GetAndMarkEventsForProcessing(Arg.Any<CancellationToken>())
            .Returns(events);

        _eventProcessor
            .ProcessEvent(Arg.Is(events[0]), Arg.Any<Dictionary<Guid, EventStatus>>())
            .Returns((events[0].Id, EventStatus.Failed, longErrorMessage));

        // Act
        await _service.ProcessEvents();

        // Assert
        ReceivedEvent processedEvent = events[0];
        Assert.True(processedEvent.ErrorMessage.Length <= 1000);
        Assert.Equal(longErrorMessage[..1000], processedEvent.ErrorMessage);
    }

    [Fact]
    public async Task ProcessEvents_ExceptionMessageContainsSingleQuote_ReplacesWithDash()
    {
        // Arrange
        List<ReceivedEvent> events = _fixture.CreateMany<ReceivedEvent>(1).ToList();
        string errorMessageWithQuotes = "Error 'message' with 'quotes'";
        
        _eventsRepository
            .GetAndMarkEventsForProcessing(Arg.Any<CancellationToken>())
            .Returns(events);

        _eventProcessor
            .ProcessEvent(Arg.Is(events[0]), Arg.Any<Dictionary<Guid, EventStatus>>())
            .Returns((events[0].Id, EventStatus.Failed, errorMessageWithQuotes));

        // Act
        await _service.ProcessEvents();

        // Assert
        ReceivedEvent processedEvent = events[0];
        Assert.Equal("Error -message- with -quotes-", processedEvent.ErrorMessage);
    }

    [Fact]
    public async Task ProcessEvents_MultipleEvents_UpdatesStatusCorrectly()
    {
        // Arrange
        List<ReceivedEvent> events = _fixture.CreateMany<ReceivedEvent>(3).ToList();
        _eventsRepository
            .GetAndMarkEventsForProcessing(Arg.Any<CancellationToken>())
            .Returns(events);

        _eventProcessor
            .ProcessEvent(Arg.Is(events[0]), Arg.Any<Dictionary<Guid, EventStatus>>())
            .Returns((events[0].Id, EventStatus.Succeeded, (string?)null));
        
        _eventProcessor
            .ProcessEvent(Arg.Is(events[1]), Arg.Any<Dictionary<Guid, EventStatus>>())
            .Returns((events[1].Id, EventStatus.Failed, "Failure reason"));
        
        _eventProcessor
            .ProcessEvent(Arg.Is(events[2]), Arg.Any<Dictionary<Guid, EventStatus>>())
            .Returns((events[2].Id, EventStatus.Failed, "Failed reason"));

        // Act
        await _service.ProcessEvents();

        // Assert
        Assert.Equal(EventStatus.Succeeded, events[0].Status);
        Assert.Null(events[0].ErrorMessage);
        
        Assert.Equal(EventStatus.Failed, events[1].Status);
        Assert.Equal("Failure reason", events[1].ErrorMessage);
        
        Assert.Equal(EventStatus.Failed, events[2].Status);
        Assert.Equal("Failed reason", events[2].ErrorMessage);
    }

    [Fact]
    public async Task ProcessEvents_CorrectLockName_UsesConsumerGroupAndServiceName()
    {
        // Arrange
        string expectedLockName = $"{_consumerGroup}-EventsProcessorService";
        SetupSuccessfulEventProcessing(new List<ReceivedEvent>());

        // Act
        await _service.ProcessEvents();

        // Assert
        _lockProvider
            .Received(1)
            .CreateLock(Arg.Is(expectedLockName));
    }

    [Fact]
    public async Task ProcessEvents_NullExceptionMessage_DoesNotThrow()
    {
        // Arrange
        List<ReceivedEvent> events = _fixture.CreateMany<ReceivedEvent>(1).ToList();
        _eventsRepository
            .GetAndMarkEventsForProcessing(Arg.Any<CancellationToken>())
            .Returns(events);

        _eventProcessor
            .ProcessEvent(Arg.Is(events[0]), Arg.Any<Dictionary<Guid, EventStatus>>())
            .Returns((events[0].Id, EventStatus.Failed, (string?)null));

        // Act
        await _service.ProcessEvents();

        // Assert
        ReceivedEvent processedEvent = events[0];
        Assert.Equal(EventStatus.Failed, processedEvent.Status);
        Assert.Null(processedEvent.ErrorMessage);
    }

    [Fact]
    public async Task ProcessEvents_EmptyExceptionMessage_SetsEmptyErrorMessage()
    {
        // Arrange
        List<ReceivedEvent> events = _fixture.CreateMany<ReceivedEvent>(1).ToList();
        string emptyMessage = string.Empty;
        
        _eventsRepository
            .GetAndMarkEventsForProcessing(Arg.Any<CancellationToken>())
            .Returns(events);

        _eventProcessor
            .ProcessEvent(Arg.Is(events[0]), Arg.Any<Dictionary<Guid, EventStatus>>())
            .Returns((events[0].Id, EventStatus.Failed, emptyMessage));

        // Act
        await _service.ProcessEvents();

        // Assert
        ReceivedEvent processedEvent = events[0];
        Assert.Equal(string.Empty, processedEvent.ErrorMessage);
    }

    [Fact]
    public async Task ProcessEvents_ProcessedEventsTracking_PassesCorrectDictionary()
    {
        // Arrange
        List<ReceivedEvent> events = _fixture.CreateMany<ReceivedEvent>(2).ToList();
        _eventsRepository
            .GetAndMarkEventsForProcessing(Arg.Any<CancellationToken>())
            .Returns(events);

        Dictionary<Guid, EventStatus>? originalDictionaryInFirstCall = null;
        Dictionary<Guid, EventStatus>? firstDictionaryPassedCaptured = null;
        _eventProcessor
            .ProcessEvent(Arg.Is(events[0]), Arg.Do<Dictionary<Guid, EventStatus>>(dict =>
            {
                firstDictionaryPassedCaptured = new  Dictionary<Guid, EventStatus>(dict);
                originalDictionaryInFirstCall = dict;
            }))
            .Returns((events[0].Id, EventStatus.Succeeded, (string?)null));

        Dictionary<Guid, EventStatus>? originalDictionaryInSecondCall = null;
        Dictionary<Guid, EventStatus>? secondDictionaryPassedCaptured = null;
        _eventProcessor
            .ProcessEvent(Arg.Is(events[1]), Arg.Do<Dictionary<Guid, EventStatus>>(dict =>
            {
                secondDictionaryPassedCaptured = new  Dictionary<Guid, EventStatus>(dict);
                originalDictionaryInSecondCall = dict;
            }))
            .Returns((events[1].Id, EventStatus.Failed, "Error"));

        // Act
        await _service.ProcessEvents();

        // Assert - Verify the first event passes an empty dictionary, second passes dictionary with first event
        await _eventProcessor
            .Received(1)
            .ProcessEvent(Arg.Is(events[0]), Arg.Any<Dictionary<Guid, EventStatus>>());

        await _eventProcessor
            .Received(1)
            .ProcessEvent(Arg.Is(events[1]), Arg.Any<Dictionary<Guid, EventStatus>>());

        originalDictionaryInFirstCall.Should().NotBeNull();
        originalDictionaryInSecondCall.Should().NotBeNull();
        Assert.Equal(originalDictionaryInFirstCall, originalDictionaryInSecondCall);

        originalDictionaryInFirstCall.Count.Should().Be(2); // I am not checking originalDictionaryInSecondCall count
        // because previous assertion ensures that they are actually same objects, so if one is 2, other is also 2.

        firstDictionaryPassedCaptured.Should().NotBeNull();
        firstDictionaryPassedCaptured.Count.Should().Be(0);

        secondDictionaryPassedCaptured.Should().NotBeNull();
        secondDictionaryPassedCaptured.Count.Should().Be(1);

        secondDictionaryPassedCaptured.Should().ContainKey(events[0].Id);
        secondDictionaryPassedCaptured[events[0].Id].Should().Be(EventStatus.Succeeded);
    }

    private void SetupSuccessfulEventProcessing(List<ReceivedEvent> events)
    {
        _eventsRepository
            .GetAndMarkEventsForProcessing(Arg.Any<CancellationToken>())
            .Returns(events);

        foreach (ReceivedEvent eventItem in events)
        {
            _eventProcessor
                .ProcessEvent(Arg.Is(eventItem), Arg.Any<Dictionary<Guid, EventStatus>>())
                .Returns((eventItem.Id, EventStatus.Succeeded, (string?)null));
        }
    }

    private async Task VerifyLockAcquisition()
    {
        _lockProvider
            .Received(1)
            .CreateLock(Arg.Is($"{_consumerGroup}-EventsProcessorService"));

        await _distributedLock
            .Received(1)
            .AcquireAsync(Arg.Is(TimeSpan.FromMinutes(2)), Arg.Is(CancellationToken.None));
    }

    private async Task VerifyEventsRetrieval()
    {
        await _eventsRepository
            .Received(1)
            .GetAndMarkEventsForProcessing(Arg.Is(CancellationToken.None));
    }

    private void VerifyEventProcessing(List<ReceivedEvent> events)
    {
        foreach (ReceivedEvent eventItem in events)
        {
            _eventProcessor
                .Received(1)
                .ProcessEvent(Arg.Is(eventItem), Arg.Any<Dictionary<Guid, EventStatus>>());
        }
    }

    private async Task VerifyEventsMarkedForPostProcessing(List<ReceivedEvent> events)
    {
        foreach (ReceivedEvent eventItem in events)
        {
            await _eventsRepository
                .Received(1)
                .MarkPostProcessing(Arg.Is(eventItem), Arg.Is(CancellationToken.None));
        }
    }
}
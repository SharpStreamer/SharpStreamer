using AutoFixture;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Storage.Npgsql;
using DotNetCore.SharpStreamer.Storage.Npgsql.Abstractions;
using Medallion.Threading;
using Microsoft.Extensions.Options;
using NSubstitute;
using Storage.Npgsql.Tests.Fixtures;

namespace Storage.Npgsql.Tests;

[Collection(nameof(GlobalCollection))]
public class EventsProcessorServiceTests
{
    private string _consumerGroup = Guid.NewGuid().ToString();
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
            .Acquire(
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .Returns(_lockHandle);
        _lockProvider.CreateLock(
            Arg.Any<string>())
            .Returns(_distributedLock);
        _lockHandle.DisposeAsync().Returns(ValueTask.CompletedTask);

        _options.Value.Returns(new SharpStreamerOptions()
        {
            ConsumerGroup = _consumerGroup,
        });
        _service = new EventsProcessorService(
            _eventsRepository,
            _eventProcessor,
            _options,
            _lockProvider);
    }

    [Fact]
    public async Task ProcessEvents_GetsEvents()
    {
        // Arrange
        Setup();

        // Act
        await _service.ProcessEvents();

        // Assert
        _lockProvider
            .Received(1)
            .CreateLock(Arg.Is($"{_consumerGroup}-EventsProcessorService"));

        await _distributedLock
            .Received(1)
            .AcquireAsync(Arg.Is(TimeSpan.FromMinutes(2)), Arg.Is(CancellationToken.None));

        await _eventsRepository
            .Received(1)
            .GetAndMarkEventsForProcessing(Arg.Is(CancellationToken.None));
    }

    private void Setup()
    {
        _eventsRepository
            .GetAndMarkEventsForProcessing(Arg.Any<CancellationToken>())
            .Returns(_fixture.CreateMany<ReceivedEvent>(1).ToList());

        _eventProcessor.ProcessEvent(Arg.Any<ReceivedEvent>(), Arg.Any<Dictionary<Guid, EventStatus>>())
            .Returns(_fixture.Create<(Guid, EventStatus, string?)>());
    }
}
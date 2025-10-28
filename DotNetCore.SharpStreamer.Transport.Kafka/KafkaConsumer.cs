using System.Diagnostics;
using Confluent.Kafka;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Transport.Kafka.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.SharpStreamer.Transport.Kafka;

public class KafkaConsumer(
    IOptions<SharpStreamerOptions> sharpStreamerOptions,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<KafkaConsumer> logger,
    ITimeService timeService,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (sharpStreamerOptions.Value.ConsumerThreadCount == 0)
        {
            throw new ArgumentException("When dealing with kafka, consumer thread count must be greater than zero.");
        }

        List<Task> consumerTasks = new();
        for (int i = 0; i < sharpStreamerOptions.Value.ConsumerThreadCount; i++)
        {
            consumerTasks.Add(Task.Run(async () => await RunConsumer(stoppingToken), CancellationToken.None));
        }
        await Task.WhenAll(consumerTasks);
    }

    private async Task RunConsumer(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope consumerScope = serviceScopeFactory.CreateAsyncScope();
                IEventsRepository eventsRepository = consumerScope.ServiceProvider.GetRequiredService<IEventsRepository>();

                ConsumerConfig config = new ConsumerConfig
                {
                    BootstrapServers = kafkaOptions.Value.Servers,
                    GroupId = sharpStreamerOptions.Value.ConsumerGroup,
                    AutoOffsetReset = AutoOffsetReset.Latest,
                    EnableAutoCommit = false,
                };

                using IConsumer<string,string> consumer = new ConsumerBuilder<string,string>(config).Build();

                consumer.Subscribe(kafkaOptions.Value.TopicsToBeConsumed);

                await Consume(consumer, eventsRepository, stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
            finally
            {
                await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
            }
        }
    }

    private async Task Consume(
        IConsumer<string,string> consumer,
        IEventsRepository eventsRepository,
        CancellationToken stoppingToken)
    {
        try
        {
            ConsumeResult<string, string>? previous = null;
            int commitCounter = 0;
            List<ReceivedEvent> receivedEvents = new();
            Stopwatch  stopwatch = new();
            stopwatch.Start();
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string,string> cr = consumer.Consume(TimeSpan.FromSeconds(1));

                if (cr is null)
                {
                    if (previous is not null && await CommitIfNecessary(stopwatch, receivedEvents, consumer, previous, eventsRepository))
                    {
                        commitCounter = 0;
                        stopwatch.Restart();
                        previous = null;
                        receivedEvents.Clear();
                    }

                    continue;
                }

                commitCounter++;
                receivedEvents.Add(BuildReceivedEventEntity(cr));
                if (await CommitIfNecessary(commitCounter, receivedEvents, consumer, cr, eventsRepository))
                {
                    commitCounter = 0;
                    stopwatch.Restart();
                    previous = null;
                    receivedEvents.Clear();
                }
                else
                {
                    previous = cr;
                }
            }

            stoppingToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning($"Consumer operation was cancelled. {ex.Message}");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task<bool> CommitIfNecessary(
        int counter,
        List<ReceivedEvent> receivedEvents,
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> cr,
        IEventsRepository eventsRepository)
    {
        if (counter == kafkaOptions.Value.CommitBatchSize)
        {
            await SaveAndCommit(receivedEvents, consumer, cr, eventsRepository);
            return true;
        }

        return false;
    }

    private async Task<bool> CommitIfNecessary(
        Stopwatch stopwatch,
        List<ReceivedEvent> receivedEvents,
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> previous,
        IEventsRepository eventsRepository)
    {
        if (stopwatch.Elapsed >= TimeSpan.FromSeconds(kafkaOptions.Value.CommitTimespanSeconds))
        {
            await SaveAndCommit(receivedEvents, consumer, previous, eventsRepository);
            return true;
        }

        return false;
    }

    private async Task SaveAndCommit(List<ReceivedEvent> receivedEvents, IConsumer<string, string> consumer, ConsumeResult<string, string> cr, IEventsRepository eventsRepository)
    {
        await SaveConsumedEvents(receivedEvents, eventsRepository);
        consumer.Commit(cr);
    }

    private async Task SaveConsumedEvents(List<ReceivedEvent> receivedEvents, IEventsRepository eventsRepository)
    {
        // Makes sure that because of fast processing, all received event won't have same Timestamp, because predecessors
        // ordering is based on Timestamp, and we should be sure that in same batch, we will have correctly ordered timestamps.
        DateTimeOffset currentTime = timeService.GetUtcNow();
        for (int i = 0; i < receivedEvents.Count; i++)
        {
            receivedEvents[i].Timestamp = currentTime.AddMilliseconds(i);
        }
        await eventsRepository.SaveConsumedEvents(receivedEvents);
    }

    private ReceivedEvent BuildReceivedEventEntity(ConsumeResult<string, string> cr)
    {
        ReceivedEvent receivedEvent = new ReceivedEvent()
        {
            Id = Guid.Parse(cr.GetHeaderValue(nameof(ReceivedEvent.Id))),
            Group = sharpStreamerOptions.Value.ConsumerGroup,
            ErrorMessage = null,
            UpdateTimestamp = null,
            Partition = cr.Partition.ToString(),
            Topic = cr.Topic,
            Content = cr.Message.Value,
            RetryCount = 0,
            SentAt = DateTimeOffset.Parse(cr.GetHeaderValue(nameof(ReceivedEvent.SentAt))),
            Timestamp = default, // Timestamp will be calculated while saving entity in database. Method: SaveConsumedEvents
            Status = EventStatus.None,
            EventKey = cr.Message.Key,
        };

        return receivedEvent;
    }
}
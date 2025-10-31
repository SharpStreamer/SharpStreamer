using System.Text;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Transport.RabbitMq.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DotNetCore.SharpStreamer.Transport.RabbitMq;

public class RabbitConsumer(
    IOptions<SharpStreamerOptions> sharpStreamerOptions,
    RabbitConnectionProvider rabbitConnectionProvider,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<RabbitConsumer> logger,
    ITimeService timeService,
    IHostApplicationLifetime applicationLifetime)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope consumerScope = serviceScopeFactory.CreateAsyncScope();
                IEventsRepository eventsRepository =
                    consumerScope.ServiceProvider.GetRequiredService<IEventsRepository>();

                IConnection connection = await rabbitConnectionProvider.GetConnectionAsync();
                await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                await DeclareQueue(channel, stoppingToken);

                // Set prefetch count to control message flow
                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 1,
                    global: false,
                    stoppingToken);
                AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);

                // ReSharper disable once AccessToDisposedClosure
                // access to disposed object won't happen because last operation is
                // await Task.Delay(Timeout.Infinite, stoppingToken); and this Infinite delay will prevent disposing
                // channel object.
                consumer.ReceivedAsync += async (sender, ea) => await OnMessageReceived(
                                                                            sender,
                                                                            ea,
                                                                            eventsRepository,
                                                                            channel,
                                                                            stoppingToken);
                await channel.BasicConsumeAsync(
                    sharpStreamerOptions.Value.ConsumerGroup,
                    autoAck: false,
                    consumer,
                    stoppingToken);

                // Keep running
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "Application is going to shutdown");
                applicationLifetime.StopApplication();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Rabbit consumer registration failed");
                applicationLifetime.StopApplication();
            }
        }
    }

    private async Task OnMessageReceived(
        object sender,
        BasicDeliverEventArgs ea,
        IEventsRepository eventsRepository,
        IChannel channel,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ReceivedEvent receivedEvent = new ReceivedEvent()
                {
                    Id = Guid.Parse(ea.GetHeaderValue(nameof(ReceivedEvent.Id))),
                    Group = sharpStreamerOptions.Value.ConsumerGroup,
                    ErrorMessage = null,
                    UpdateTimestamp = null,
                    Partition = "0",
                    Topic = ea.Exchange,
                    Content = Encoding.UTF8.GetString(ea.Body.Span),
                    RetryCount = 0,
                    SentAt = DateTimeOffset.Parse(ea.GetHeaderValue(nameof(ReceivedEvent.SentAt))),
                    Timestamp = timeService.GetUtcNow(),
                    Status = EventStatus.None,
                    EventKey = ea.RoutingKey,
                };
                await eventsRepository.SaveConsumedEvents([ receivedEvent ], CancellationToken.None);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: true, stoppingToken);
                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Rabbit consumer fails to save message in database");
            }
        }
    }

    private async Task DeclareQueue(IChannel channel, CancellationToken stoppingToken)
    {
        await channel.QueueDeclareAsync(
            queue: sharpStreamerOptions.Value.ConsumerGroup,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>()
            {
                { "x-single-active-consumer", true }
            },
            noWait: false,
            cancellationToken: stoppingToken);
    }
}
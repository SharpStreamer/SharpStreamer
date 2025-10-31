using DotNetCore.SharpStreamer.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DotNetCore.SharpStreamer.Transport.RabbitMq;

internal class SetupRabbitInfrastructureHostedService(
    IOptions<RabbitOptions> rabbitOptions,
    IOptions<SharpStreamerOptions> sharpStreamerOptions,
    RabbitConnectionProvider rabbitConnectionProvider,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<SetupRabbitInfrastructureHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            IConnection connection = await rabbitConnectionProvider.GetConnectionAsync();
            await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await DeclareExchanges(channel, stoppingToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Rabbit Setup failed");
            hostApplicationLifetime.StopApplication();
        }
    }

    private async Task DeclareExchanges(IChannel channel, CancellationToken stoppingToken)
    {
        foreach (string topic in rabbitOptions.Value.Topics)
        {
            await channel.ExchangeDeclareAsync(
                exchange: topic,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false,
                arguments: null,
                passive: false,
                noWait: false, // noWait : true means that we don't wait for response from the server. in this case we wait
                cancellationToken: stoppingToken);
            await channel.QueueBindAsync(
                queue: sharpStreamerOptions.Value.ConsumerGroup,
                exchange: topic,
                routingKey: "",
                arguments: null,
                noWait: false, 
                cancellationToken: stoppingToken);
        }
    }
}
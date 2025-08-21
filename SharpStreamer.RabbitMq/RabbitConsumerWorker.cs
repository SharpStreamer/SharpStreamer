using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Stream.Client;
using SharpStreamer.Abstractions.Services.Abstractions;

namespace SharpStreamer.RabbitMq;

public class RabbitConsumerWorker(
    ILogger<RabbitConsumerWorker> logger,
    ILogger<StreamSystem> streamSystemLogger,
    IOptions<RabbitConfig> rabbitConfig,
    IMetadataService metadataService
    ) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        StreamSystem streamSystem = await StreamSystem.Create(
            new StreamSystemConfig()
            {
                UserName = rabbitConfig.Value.UserName,
                Password = rabbitConfig.Value.Password,
                VirtualHost = rabbitConfig.Value.VirtualHost,
                Endpoints = rabbitConfig.Value.Addresses.Select(addr => (EndPoint)new IPEndPoint(IPAddress.Parse(addr.Ip), addr.Port)).ToList(),
                ConnectionPoolConfig = new ConnectionPoolConfig()
                {
                    ConsumersPerConnection = 10,
                    ProducersPerConnection = 10,
                    ConnectionCloseConfig = new ConnectionCloseConfig()
                    {
                        IdleTime = TimeSpan.FromMinutes(5),
                        Policy = ConnectionClosePolicy.CloseWhenEmptyAndIdle,
                    }
                }
            }, streamSystemLogger);
        
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
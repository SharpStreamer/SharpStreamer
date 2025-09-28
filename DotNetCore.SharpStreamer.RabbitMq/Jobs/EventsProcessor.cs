using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.Extensions.Hosting;

namespace DotNetCore.SharpStreamer.RabbitMq.Jobs;

public class EventsProcessor(IConsumerService consumerService) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}
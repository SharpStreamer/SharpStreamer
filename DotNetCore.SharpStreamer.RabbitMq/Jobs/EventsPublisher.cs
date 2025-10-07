using Microsoft.Extensions.Hosting;

namespace DotNetCore.SharpStreamer.RabbitMq.Jobs;

public class EventsPublisher : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}
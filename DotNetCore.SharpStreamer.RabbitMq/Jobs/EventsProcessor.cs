using Microsoft.Extensions.Hosting;

namespace DotNetCore.SharpStreamer.RabbitMq.Jobs;

public class EventsProcessor : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}
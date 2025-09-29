using DotNetCore.SharpStreamer.Options;
using DotNetCore.SharpStreamer.RabbitMq.Options;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.SharpStreamer.RabbitMq.Jobs;

public class EventsProcessor(
    IServiceScopeFactory serviceScopeFactory,
    ITimeService timeService,
    ILogger<EventsProcessor> logger,
    IOptions<SharpStreamerOptions<TopicOptions>> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<Task> processorTasks = new List<Task>();

        for (int i = 0; i < options.Value.ProcessorThreadCount; i++)
        {
            processorTasks.Add(Task.Run(async () => await RunProcessor(stoppingToken), stoppingToken));
        }

        await Task.WhenAll(processorTasks);
    }

    private async Task RunProcessor(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
                IConsumerService consumerService = scope.ServiceProvider.GetRequiredService<IConsumerService>();

            }
            catch (Exception fin)
            {
                logger.LogError(fin, "Error in processing events");
                await timeService.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
            }
        }
    }
}
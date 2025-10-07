using DotNetCore.SharpStreamer.RabbitMq.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.SharpStreamer.RabbitMq;

public static class SharpStreamerRabbitMqExtensions
{
    public static IServiceCollection AddSharpStreamerRabbitMq(this IServiceCollection services)
    {
        services.AddHostedService<EventsPublisher>();
        return services;
    }
}
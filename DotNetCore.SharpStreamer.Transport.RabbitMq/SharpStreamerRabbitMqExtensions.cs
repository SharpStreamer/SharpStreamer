using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.SharpStreamer.Transport.RabbitMq;

public static class SharpStreamerRabbitMqExtensions
{
    public static IServiceCollection AddSharpStreamerRabbitMq(this IServiceCollection services)
    {
        services.AddSingleton<ITransportService, TransportRabbitMqtService>();
        return services;
    }
}
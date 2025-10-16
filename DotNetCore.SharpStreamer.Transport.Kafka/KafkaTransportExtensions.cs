using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.SharpStreamer.Transport.Kafka;

public static class KafkaTransportExtensions
{
    public static IServiceCollection AddSharpStreamerTransportKafka(this IServiceCollection services)
    {
        services.AddSingleton<ITransportService, KafkaTransportService>();
        services.AddHostedService<KafkaConsumer>();
        return services;
    }
}
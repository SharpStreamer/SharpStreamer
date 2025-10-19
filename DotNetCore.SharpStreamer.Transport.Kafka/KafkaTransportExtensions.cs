using DotNetCore.SharpStreamer.Services;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.SharpStreamer.Transport.Kafka;

public static class KafkaTransportExtensions
{
    private const string KafkaConfigName = "Kafka";
    public static IServiceCollection AddSharpStreamerTransportKafka(this IServiceCollection services)
    {
        services.AddSingleton<ITransportService, KafkaTransportService>();
        services.AddHostedService<KafkaConsumer>();

        string coreConfigurationName = DiService.ConfigurationSectionName ??
                                       throw new ArgumentException(
                                           "SharpStreamer's core library is not added correctly");
        services.AddOptions<KafkaOptions>()
            .BindConfiguration($"{coreConfigurationName}:{KafkaConfigName}")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services;
    }
}
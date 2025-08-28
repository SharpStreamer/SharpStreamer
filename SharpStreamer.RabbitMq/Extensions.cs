using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharpStreamer.Abstractions.Services;
using SharpStreamer.Abstractions.Services.Abstractions;

namespace SharpStreamer.RabbitMq;

public static class Extensions
{
    public static IServiceCollection AddSharpStreamerRabbitMq(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddOptions<RabbitConfig>()
            .BindConfiguration("SharpStreamerConfig:RabbitConfig")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        IMetadataService metadataService = new MetadataService();
        metadataService.AddServicesAndCache(services, assemblies);
        services.AddSingleton(metadataService);

        services.AddHostedService<RabbitConsumerWorker>();
        return services;
    }
}
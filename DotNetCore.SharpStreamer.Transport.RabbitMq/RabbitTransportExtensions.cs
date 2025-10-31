using DotNetCore.SharpStreamer.Services;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.SharpStreamer.Transport.RabbitMq;

public static class RabbitTransportExtensions
{
    private const string RabbitConfigName = "RabbitMq";

    public static IServiceCollection AddSharpStreamerTransportRabbitMq(this IServiceCollection services)
    {
        string coreConfigurationName = DiService.ConfigurationSectionName ??
                                       throw new ArgumentException(
                                           "SharpStreamer's core library is not added correctly");
        services.AddOptions<RabbitOptions>()
            .BindConfiguration($"{coreConfigurationName}:{RabbitConfigName}")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<RabbitConnectionProvider>();
        services.AddHostedService<SetupRabbitInfrastructureHostedService>();
        services.AddScoped<ITransportService, RabbitTransportService>();
        services.AddHostedService<RabbitConsumer>();
        return services;
    }
}
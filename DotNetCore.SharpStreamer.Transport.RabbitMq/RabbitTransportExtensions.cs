using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.SharpStreamer.Transport.RabbitMq;

public static class RabbitTransportExtensions
{
    private const string RabbitConfigName = "RabbitMq";

    public static IServiceCollection AddSharpStreamerTransportRabbitMq(this IServiceCollection services)
    {
        SetupInfrastructure();
        return services;
    }

    private static void SetupInfrastructure()
    {
        throw new NotImplementedException();
    }
}
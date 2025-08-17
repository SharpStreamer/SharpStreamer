using SharpStreamer.Abstractions;

namespace SharpStreamer.RabbitMq.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses.Users;

public class UserCreatedHandler : IConsumer<UserCreated>
{
    public Task Handle(UserCreated request, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"{nameof(UserCreated)} handled by {nameof(UserCreatedHandler)}");
        return Task.CompletedTask;
    }
}
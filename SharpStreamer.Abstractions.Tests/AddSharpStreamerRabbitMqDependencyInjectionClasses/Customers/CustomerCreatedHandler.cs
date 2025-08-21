namespace SharpStreamer.Abstractions.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses.Customers;

public class CustomerCreatedHandler : IConsumer<CustomerCreated>
{
    public Task Handle(CustomerCreated request, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"{nameof(CustomerCreated)} handled by {nameof(CustomerCreatedHandler)}");
        return Task.CompletedTask;
    }
}
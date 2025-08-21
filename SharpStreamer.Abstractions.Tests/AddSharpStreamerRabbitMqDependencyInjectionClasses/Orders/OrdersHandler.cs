namespace SharpStreamer.Abstractions.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses.Orders;

public class OrdersHandler : 
    IConsumer<OrderSubmitted>, IConsumer<OrderCreated>, IConsumer<OrderShipped>
{
    public Task Handle(OrderSubmitted request, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"{nameof(OrderSubmitted)} handled by {nameof(OrdersHandler)}");
        return Task.CompletedTask;
    }

    public Task Handle(OrderCreated request, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"{nameof(OrderCreated)} handled by {nameof(OrdersHandler)}");
        return Task.CompletedTask;
    }

    public Task Handle(OrderShipped request, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"{nameof(OrderShipped)} handled by {nameof(OrdersHandler)}");
        return Task.CompletedTask;
    }
}
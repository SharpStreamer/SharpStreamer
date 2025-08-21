using SharpStreamer.Abstractions.Attributes;

namespace SharpStreamer.Abstractions.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses.Orders;

[ConsumeEvent("order_shipped", "tests_consumer_group_1")]
public class OrderShipped : IEvent
{
    
}
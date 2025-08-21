using SharpStreamer.Abstractions.Attributes;

namespace SharpStreamer.Abstractions.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses.Orders;

[ConsumeEvent("order_submitted", "tests_consumer_group_1")]
public class OrderSubmitted : IEvent
{
    
}
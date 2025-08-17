using SharpStreamer.Abstractions;
using SharpStreamer.Abstractions.Attributes;

namespace SharpStreamer.RabbitMq.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses.Orders;

[ConsumeEvent("order_submitted", "tests_consumer_group_1")]
public class OrderSubmitted : IEvent
{
    
}
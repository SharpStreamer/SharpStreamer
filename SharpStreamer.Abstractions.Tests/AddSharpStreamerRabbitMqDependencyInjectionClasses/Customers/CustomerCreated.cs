using SharpStreamer.Abstractions.Attributes;

namespace SharpStreamer.Abstractions.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses.Customers;

[ConsumeEvent("customer_created", "tests_consumer_group_2")]
public class CustomerCreated : IEvent
{
    
}
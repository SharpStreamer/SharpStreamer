using DotNetCore.SharpStreamer.Bus.Attributes;
using Mediator;

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.EventsAndHandlers.CustomerCreated;

[ConsumeEvent("customer_created")]
[PublishEvent("identity", "customer_created")]
public class CustomerCreatedEvent : IRequest
{
    
}
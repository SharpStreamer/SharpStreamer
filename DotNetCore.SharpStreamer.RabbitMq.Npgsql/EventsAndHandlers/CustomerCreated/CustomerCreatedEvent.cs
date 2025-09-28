using DotNetCore.SharpStreamer.Bus.Attributes;
using Mediator;

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.EventsAndHandlers.CustomerCreated;

[ConsumeEvent("customer_created")]
[PublishEvent("customer_created", "identity")]
public class CustomerCreatedEvent : IRequest
{
    public required string PersonalNumber { get; set; }
}
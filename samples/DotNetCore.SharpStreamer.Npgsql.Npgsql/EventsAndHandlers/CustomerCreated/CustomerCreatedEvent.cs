using DotNetCore.SharpStreamer.Bus.Attributes;
using MediatR;

namespace DotNetCore.SharpStreamer.Npgsql.Npgsql.EventsAndHandlers.CustomerCreated;

[ConsumeEvent("customer_created", nextRetryInSeconds: 5)]
[PublishEvent("customer_created", "identity")]
public class CustomerCreatedEvent : IRequest
{
    public required string PersonalNumber { get; set; }
}
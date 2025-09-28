using DotNetCore.SharpStreamer.Bus.Attributes;
using Mediator;

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.EventsAndHandlers.UserCreated;

[ConsumeEvent("user_created")]
[ProduceEvent("identity", "user_created")]
public class UserCreatedEvent : IRequest
{
    
}
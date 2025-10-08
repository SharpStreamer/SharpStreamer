using DotNetCore.SharpStreamer.Bus.Attributes;
using MediatR;

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.EventsAndHandlers.UserCreated;

[ConsumeEvent("user_created")]
[PublishEvent("identity", "user_created")]
public class UserCreatedEvent : IRequest
{
    
}
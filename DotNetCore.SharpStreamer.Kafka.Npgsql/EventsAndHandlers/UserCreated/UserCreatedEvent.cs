using DotNetCore.SharpStreamer.Bus.Attributes;
using MediatR;

namespace DotNetCore.SharpStreamer.Kafka.Npgsql.EventsAndHandlers.UserCreated;

[ConsumeEvent("user_created")]
[PublishEvent("user_created", "users")]
public class UserCreatedEvent : IRequest
{
    
}
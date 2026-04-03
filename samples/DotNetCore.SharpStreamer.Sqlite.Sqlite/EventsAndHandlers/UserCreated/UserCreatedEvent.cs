using DotNetCore.SharpStreamer.Bus.Attributes;
using MediatR;

namespace DotNetCore.SharpStreamer.Sqlite.Sqlite.EventsAndHandlers.UserCreated;

[ConsumeEvent("user_created")]
[PublishEvent("user_created", "identity")]
public class UserCreatedEvent : IRequest
{
    
}
using SharpStreamer.Abstractions;
using SharpStreamer.Abstractions.Attributes;

namespace SharpStreamer.Api.EventsAndHandlers;


[ConsumeEvent("user_created", "test_group_1")]
public class UserCreated : IEvent
{
    public Guid UserId { get; set; }

    public string UserName { get; set; }
}


public class UserCreatedHandler : IConsumer<UserCreated>
{
    public Task Handle(UserCreated request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
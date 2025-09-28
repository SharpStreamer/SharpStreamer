using Mediator;

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.EventsAndHandlers.UserCreated;

public class UserCreatedEventHandler : BaseEventHandler<UserCreatedEvent>
{
    protected override ValueTask HandleEvent(UserCreatedEvent request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
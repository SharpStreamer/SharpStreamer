using MediatR;

namespace DotNetCore.SharpStreamer.Npgsql.Npgsql.EventsAndHandlers.UserCreated;

public class UserCreatedEventHandler : IRequestHandler<UserCreatedEvent>
{
    public Task Handle(UserCreatedEvent request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
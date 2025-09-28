using Mediator;

namespace DotNetCore.SharpStreamer;

public abstract class BaseEventHandler<TEvent> : IRequestHandler<TEvent>
    where TEvent : IRequest
{
    public async ValueTask<Unit> Handle(TEvent request, CancellationToken cancellationToken)
    {
        await HandleEvent(request, cancellationToken);
        return Unit.Value;
    }

    protected abstract ValueTask HandleEvent(TEvent request, CancellationToken cancellationToken);
}
using Mediator;

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.EventsAndHandlers.CustomerCreated;

public class CustomerCreatedEventHandler : IRequestHandler<CustomerCreatedEvent>
{
    public ValueTask<Unit> Handle(CustomerCreatedEvent request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
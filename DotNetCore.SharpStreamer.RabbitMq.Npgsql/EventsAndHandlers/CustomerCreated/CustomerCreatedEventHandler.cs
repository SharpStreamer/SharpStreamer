using Mediator;

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.EventsAndHandlers.CustomerCreated;

public class CustomerCreatedEventHandler : BaseEventHandler<CustomerCreatedEvent>
{
    protected override ValueTask HandleEvent(CustomerCreatedEvent request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
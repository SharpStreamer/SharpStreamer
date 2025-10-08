using MediatR;

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.EventsAndHandlers.CustomerCreated;

public class CustomerCreatedEventHandler : IRequestHandler<CustomerCreatedEvent>
{
    public Task Handle(CustomerCreatedEvent request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
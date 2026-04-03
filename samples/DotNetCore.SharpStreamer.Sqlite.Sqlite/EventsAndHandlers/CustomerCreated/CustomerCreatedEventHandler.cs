using MediatR;

namespace DotNetCore.SharpStreamer.Sqlite.Sqlite.EventsAndHandlers.CustomerCreated;

public class CustomerCreatedEventHandler(ILogger<CustomerCreatedEventHandler> logger) : IRequestHandler<CustomerCreatedEvent>
{
    public Task Handle(CustomerCreatedEvent request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Hello World!");
        return Task.CompletedTask;
    }
}
using SharpStreamer.Abstractions;
using SharpStreamer.Abstractions.Attributes;

namespace SharpStreamer.Api.EventsAndHandlers;

[ConsumeEvent("customer_created", "test_group_1")]
public class CustomerCreated : IEvent
{
    public Guid CustomerId { get; set; }

    public string PersonalNumber { get; set; }
}


public class CustomerCreatedHandler : IConsumer<CustomerCreated>
{
    public Task Handle(CustomerCreated request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
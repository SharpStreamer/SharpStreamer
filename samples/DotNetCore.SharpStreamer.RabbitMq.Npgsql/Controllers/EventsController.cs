using DotNetCore.SharpStreamer.Bus;
using DotNetCore.SharpStreamer.RabbitMq.Npgsql.EventsAndHandlers.CustomerCreated;
using Microsoft.AspNetCore.Mvc;

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(IStreamerBus bus) : ControllerBase
{
    [HttpPost("customer-created")]
    public async Task<IActionResult> PublishCustomerCreatedEvent()
    {
        await bus.PublishAsync(new CustomerCreatedEvent()
        {
            PersonalNumber = "111",
        }, "key-1");
        return Ok();
    }

    [HttpPost("customer-created-delayed")]
    public async Task<IActionResult> PublishCustomerCreatedDelayedEvent()
    {
        await bus.PublishDelayedAsync(new CustomerCreatedEvent()
        {
            PersonalNumber = "111",
        }, TimeSpan.FromMinutes(1));
        return Ok();
    }

    [HttpPost("non-legit")]
    public async Task<IActionResult> PublishNonLegitEvent()
    {
        await bus.PublishAsync(new EventWithoutProduceAttribute(), "key-2");
        return Ok();
    }
}
public class EventWithoutProduceAttribute;
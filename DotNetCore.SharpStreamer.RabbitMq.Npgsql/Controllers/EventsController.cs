using Microsoft.AspNetCore.Mvc;

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    [HttpPost]
    public Task<IActionResult> PublishEvent()
    {
        return Task.FromResult((IActionResult)Ok());
    }
}
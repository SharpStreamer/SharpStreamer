using DotNetCore.SharpStreamer.Bus;
using DotNetCore.SharpStreamer.DistributedUnitOfWork;
using DotNetCore.SharpStreamer.Npgsql.Npgsql.EventsAndHandlers.CustomerCreated;
using Microsoft.AspNetCore.Mvc;

namespace DotNetCore.SharpStreamer.Npgsql.Npgsql.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(
    IStreamerBus bus,
    IDistributedUnitOfWork distributedUnitOfWork) : ControllerBase
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

    [HttpPost("distributed-unit-of-work/{name}")]
    public async Task<IActionResult> DistributedUnitOfWorkExample(
        [FromRoute] string name,
        [FromQuery] bool failFirstOperation = false,
        [FromQuery] bool failSecondOperation = false,
        [FromQuery] bool failAtCommitPhase = false)
    {
        await using IDistributedTransaction<RollbackService> transaction =
            await distributedUnitOfWork.BeginDistributedTransactionAsync<RollbackService>(name);

        if (failFirstOperation)
        {
            throw new Exception("First operationFail fail");
        }

        Console.WriteLine("First operation Success");
        transaction.Push(new RollbackService.RollbackOperation1{Name = "SandrikaMgeli"});

        if (failSecondOperation)
        {
            throw new Exception("Second operationFail fail");
        }

        Console.WriteLine("Second operation Success");
        transaction.Push(new RollbackService.RollbackOperation2{Age = 22});

        if (failAtCommitPhase)
        {
            throw new Exception("Commit phase fail");
        }
        await transaction.CommitAsync();

        return Ok();
    }
}
public class EventWithoutProduceAttribute;

public class RollbackService
    : IRollbackHandler<RollbackService.RollbackOperation1>, 
      IRollbackHandler<RollbackService.RollbackOperation2>
{

    public class RollbackOperation1
    {
        public string Name { get; set; }
    }

    public class RollbackOperation2
    {
        public int Age { get; set; }
    }

    public Task HandleRollback(RollbackOperation1 data, CancellationToken cancellationToken = default)
    {
        Console.WriteLine(data.Name + " From RollbackOperation1");
        return Task.CompletedTask;
    }

    public Task HandleRollback(RollbackOperation2 data, CancellationToken cancellationToken = default)
    {
        Console.WriteLine(data.Age + " From RollbackOperation2");
        return Task.CompletedTask;
    }
}
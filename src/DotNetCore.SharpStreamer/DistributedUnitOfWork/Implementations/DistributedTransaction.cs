using DotNetCore.SharpStreamer.Bus;
using DotNetCore.SharpStreamer.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.SharpStreamer.DistributedUnitOfWork.Implementations;

internal class DistributedTransaction<TRollbackService>(
    string transactionName,
    IServiceScopeFactory serviceScopeFactory)
    : IDistributedTransaction<TRollbackService>
    where TRollbackService : class
{
    private Stack<(string operationName, object data)> _operationsStack = new();
    private bool _isCommited;
    private bool _isDisposed;

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException("DistributedTransaction", "Cannot disposed already disposed object");
        }

        _isDisposed = true;

        // If Dispose was called without commiting, we assume that transaction failed, so we will publish reconcilatin events.
        if (!_isCommited)
        {
            await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
            IStreamerBus streamerBus = scope.ServiceProvider.GetRequiredService<IStreamerBus>();
            ProcessRollbackTransactionCommand command = new ProcessRollbackTransactionCommand(_operationsStack, transactionName);
            await streamerBus.PublishAsync(command, Guid.NewGuid().ToString());
        }
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_isCommited)
        {
            throw new InvalidOperationException("Transaction is already committed");
        }

        if (_isDisposed)
        {
            throw new ObjectDisposedException("DistributedTransaction", "Cannot commit when disposed");
        }

        _isCommited = true;
        return Task.CompletedTask;
    }

    public void Push<TRollbackData1>(TRollbackData1 data) where TRollbackData1 : class
    {
        if (!_operationsStack.Any())
        {
            _operationsStack.Push(
                (
                    DistributedUnitOfWorkConstants.NameOfUnlockDistributedTransactionHandler,
                    new ReleaseDistributedTransactionLock{ Name = transactionName}
                ));
        }
    }
}
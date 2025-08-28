using System.Data;

namespace SharpStreamer.Abstractions;

public interface IUnitOfWork
{
    Task<IAsyncTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

    ITransaction BeginTransaction(IsolationLevel isolationLevel);
}
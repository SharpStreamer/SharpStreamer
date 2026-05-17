namespace DotNetCore.SharpStreamer.DistributedUnitOfWork;

public interface IDistributedUnitOfWork
{
    /// <typeparam name="TRollbackService">Rollback service is service where rollback instructions are defined.</typeparam>
    Task<IDistributedTransaction<TRollbackService>> BeginDistributedTransactionAsync<TRollbackService>(
        string transactionName,
        CancellationToken cancellationToken = default)
        where  TRollbackService : class;
}
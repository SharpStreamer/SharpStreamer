namespace DotNetCore.SharpStreamer.DistributedUnitOfWork;


/// <typeparam name="TRollbackService">Rollback service is service where rollback instructions are defined.</typeparam>
public interface IDistributedTransaction<TRollbackService> : IAsyncDisposable
    where TRollbackService : class
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    void Push<TRollbackData>(TRollbackData data) where TRollbackData : class;
}
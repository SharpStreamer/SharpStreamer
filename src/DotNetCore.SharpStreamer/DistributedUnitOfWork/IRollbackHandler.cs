namespace DotNetCore.SharpStreamer.DistributedUnitOfWork;

public interface IRollbackHandler<in TRollbackData>
    where TRollbackData : class
{
    Task HandleRollback(TRollbackData data, CancellationToken cancellationToken = default);
}
using DotNetCore.SharpStreamer.Bus.Attributes;
using MediatR;

namespace DotNetCore.SharpStreamer.DistributedUnitOfWork.Implementations;

#nullable disable
[PublishEvent("distributed-transactions-rollback-operation", "")]
internal class ProcessRollbackTransactionCommand : IRequest
{
    public ProcessRollbackTransactionCommand(Stack<(string operationName, object data)> operationsStack,
        string transactionName)
    {
        TransactionName = transactionName;
        RollbackOperations = [];
        foreach ((string operationName, object data) in operationsStack)
        {
            RollbackOperations.Add(new RollbackUnit { OperationName = operationName, Data = data });
        }
    }

    public string TransactionName { get; private set; }

    // ReSharper disable once CollectionNeverQueried.Global
    public List<RollbackUnit> RollbackOperations { get; private set; }

    internal class RollbackUnit
    {
        public string OperationName { get; set; }

        public object Data { get; set; }
    }
}
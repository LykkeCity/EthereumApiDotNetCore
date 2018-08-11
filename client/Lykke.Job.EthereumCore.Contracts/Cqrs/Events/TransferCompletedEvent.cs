using System;
using MessagePack;

namespace Lykke.Job.EthereumCore.Contracts.Cqrs.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class TransferCompletedEvent
    {
        public Guid OperationId { get; set; }
    }
}
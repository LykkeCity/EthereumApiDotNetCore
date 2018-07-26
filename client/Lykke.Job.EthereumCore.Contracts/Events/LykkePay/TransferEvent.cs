using System;
using Lykke.Job.EthereumCore.Contracts.Enums.LykkePay;

namespace Lykke.Job.EthereumCore.Contracts.Events.LykkePay
{
    public class TransferEvent
    {
        public DateTime DetectedTime { get; private set; }
        public string TransactionHash { get; private set; }
        public string Amount { get; private set; }
        public string TokenAddress { get; private set; }
        public string FromAddress { get; private set; }
        public string ToAddress { get; private set; }
        public SenderType SenderType { get; private set; }
        public EventType EventType { get; private set; }
        public string OperationId { get; private set; }
        public string BlockHash { get; private set; }
        public ulong BlockNumber { get; private set; }
        public WorkflowType WorkflowType { get; private set; }

        public TransferEvent(string operationId,
            string transactionHash, 
            string amount, 
            string tokenAddress, 
            string fromAddress, 
            string toAddress,
            string blockHash,
            ulong blockNumber,
            SenderType senderType,
            EventType eventType,
            WorkflowType workflowType,
            DateTime detectedTime)
        {
            OperationId = operationId;
            DetectedTime = detectedTime;
            TransactionHash = transactionHash;
            Amount = amount;
            TokenAddress = tokenAddress;
            FromAddress = fromAddress;
            ToAddress = toAddress;
            SenderType = senderType;
            EventType = eventType;
            BlockHash = blockHash;
            BlockNumber = blockNumber;
            WorkflowType = workflowType;
        }
    }
}

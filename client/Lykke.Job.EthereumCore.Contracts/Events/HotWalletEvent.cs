using Lykke.Job.EthereumCore.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Job.EthereumCore.Contracts.Events
{
    public class HotWalletEvent
    {
        public string OperationId { get; private set; }
        public string TransactionHash { get; private set; }
        public string FromAddress { get; private set; }
        public string ToAddress { get; private set; }
        public string Amount { get; private set; }
        public DateTime EventTime { get; private set; }
        public HotWalletEventType EventType { get; private set; }
        public string TokenAddress { get; private set; }

        public HotWalletEvent(string operationId,
            string transactionHash,
            string fromAddress,
            string toAddress,
            string amount,
            string tokenAddress,
            HotWalletEventType eventType)
        {
            OperationId = operationId;
            TransactionHash = transactionHash;
            FromAddress = fromAddress;
            ToAddress = toAddress;
            Amount = amount;
            EventTime = DateTime.UtcNow;
            EventType = eventType;
            TokenAddress = tokenAddress;
        }
    }
}

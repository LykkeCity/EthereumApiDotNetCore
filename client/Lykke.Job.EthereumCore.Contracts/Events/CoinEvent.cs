using Lykke.Job.EthereumCore.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Job.EthereumCore.Contracts.Events
{
    public class CoinEvent
    {
        public string OperationId          { get; set; }
        public CoinEventType CoinEventType { get; set; }
        public string TransactionHash      { get; set; }
        public string ContractAddress      { get; private set; }
        public string FromAddress          { get; private set; }
        public string ToAddress            { get; private set; }
        public string Amount               { get; set; }
        public string Additional           { get; private set; }
        public DateTime EventTime          { get; private set; }
        public bool Success                { get; set; }

        public CoinEvent(string operationId, 
            string transactionHash, 
            string fromAddress, 
            string toAddress,
            string amount, 
            CoinEventType coinEventType,
            string contractAddress = "",
            bool success = true, 
            string additional = "")
        {
            OperationId     = operationId;
            TransactionHash = transactionHash;
            FromAddress     = fromAddress;
            ToAddress       = toAddress;
            Amount          = amount;
            CoinEventType   = coinEventType;
            ContractAddress = contractAddress;
            Success         = success;
            Additional      = additional;
            EventTime       = DateTime.UtcNow;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Job.EthereumCore.Contracts.Events.LykkePay
{
    public abstract class Erc20TransferBase
    {
        public DateTime DetectedTime { get; protected set; }
        public string TransactionHash { get; protected set; }
        public string Amount { get; protected set; }
        public string TokenAddress { get; protected set; }
        public string FromAddress { get; protected set; }
        public string ToAddress { get; protected set; }

        public Erc20TransferBase(string transactionHash,
            string amount,
            string tokenAddress,
            string fromAddress,
            string toAddress)
        {
            DetectedTime = DateTime.UtcNow;
            TransactionHash = transactionHash;
            Amount = amount;
            TokenAddress = tokenAddress;
            FromAddress = fromAddress;
            ToAddress = toAddress;
        }
    }
}

using System;
using Lykke.Job.EthereumCore.Contracts.Enums.LykkePay;

namespace Lykke.Job.EthereumCore.Contracts.Events.LykkePay
{
    /// <summary>
    /// Event is fired after there is at least 1 confirmation of the transfer to LykkePay address
    /// </summary>
    public class TransferEvent : Erc20TransferBase
    {
        public TransferEvent(string operationId,
            string transactionHash, 
            string amount, 
            string tokenAddress, 
            string fromAddress, 
            string toAddress,
            SenderType senderType,
            EventType eventType) : base(operationId, transactionHash, amount, tokenAddress, fromAddress, toAddress, senderType, eventType)
        {
        }
    }
}

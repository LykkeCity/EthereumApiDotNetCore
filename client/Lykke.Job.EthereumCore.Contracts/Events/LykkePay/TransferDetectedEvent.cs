using System;
using Lykke.Job.EthereumCore.Contracts.Enums.LykkePay;

namespace Lykke.Job.EthereumCore.Contracts.Events.LykkePay
{
    /// <summary>
    /// Event is fired after there is at least 1 confirmation of the transfer to LykkePay address
    /// </summary>
    public class TransferDetectedEvent : Erc20TransferBase
    {
        public TransferDetectedEvent(string transactionHash, 
            string amount, 
            string tokenAddress, 
            string fromAddress, 
            string toAddress,
            int confirmationNumber,
            SenderType senderType) : base(transactionHash, amount, tokenAddress, fromAddress, toAddress, confirmationNumber, senderType)
        {
        }
    }
}

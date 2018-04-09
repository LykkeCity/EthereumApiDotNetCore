using System;

namespace Lykke.Job.EthereumCore.Contracts.Events.LykkePay
{
    /// <summary>
    /// Event is fired after there is at least 1 confirmation of the transfer to LykkePay address
    /// </summary>
    public class TransferReceivedEvent : Erc20TransferBase
    {
        public TransferReceivedEvent(string transactionHash, 
            string amount, 
            string tokenAddress, 
            string fromAddress, 
            string toAddress) : base(transactionHash, amount, tokenAddress, fromAddress, toAddress)
        {
        }
    }
}

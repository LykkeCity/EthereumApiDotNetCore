using System;

namespace Lykke.Job.EthereumCore.Contracts.Events.LykkePay
{
    /// <summary>
    /// Event is fired after transaction has been included in to the blockchain with conf lvl of 3
    /// </summary>
    public class TransferCompletedEvent : Erc20TransferBase
    {
        public TransferCompletedEvent(string transactionHash, 
            string amount, 
            string tokenAddress, 
            string fromAddress, 
            string toAddress) : base(transactionHash, amount, tokenAddress, fromAddress, toAddress)
        {
        }
    }
}

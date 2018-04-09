using System;

namespace Lykke.Job.EthereumCore.Contracts.Events.LykkePay
{
    /// <summary>
    /// Event is fired after transfer process have been started from LykkePay address
    /// </summary>
    public class TransferStartedEvent : Erc20TransferBase
    {
        public TransferStartedEvent(string transactionHash, 
            string amount, 
            string tokenAddress, 
            string fromAddress, 
            string toAddress) : base(transactionHash, amount, tokenAddress, fromAddress, toAddress)
        {
        }
    }
}

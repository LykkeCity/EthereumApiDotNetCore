using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.LykkePay
{
    public interface ILykkePayEventsService
    {
        Task IndexCashinEventsForErc20Deposits();
        Task<(BigInteger? amount, string blockHash, ulong blockNumber)> 
            IndexCashinEventsForErc20TransactionHashAsync(string transactionHash);
    }
}

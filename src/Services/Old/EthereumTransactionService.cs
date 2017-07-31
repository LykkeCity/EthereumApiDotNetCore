using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Settings;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Core;

namespace Services
{
    public interface IEthereumTransactionService
    {
        Task<bool> IsTransactionInPool(string transactionHash);
        Task<bool> IsTransactionExecuted(string hash, int gasSended);
        Task<TransactionReceipt> GetTransactionReceipt(string transaction);
    }

    public class EthereumTransactionService : IEthereumTransactionService
    {
        private readonly IBaseSettings _settings;
        private readonly Web3 _client;

        public EthereumTransactionService(IBaseSettings settings, Web3 client)
        {
            _client = client;
            _settings = settings;
        }

        // Do not use with private wallets
        public async Task<bool> IsTransactionExecuted(string hash, int gasSended)
        {
            var receipt = await GetTransactionReceipt(hash);

            if (receipt == null)
                return false;

            if (receipt.GasUsed.Value != new Nethereum.Hex.HexTypes.HexBigInteger(gasSended).Value)
                return true;

            return false;
        }


        public async Task<TransactionReceipt> GetTransactionReceipt(string transaction)
        {
            return await _client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transaction);
        }

        public async Task<bool> IsTransactionInPool(string transactionHash)
        {
            Transaction transaction = await _client.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionHash);
            if (transaction == null || (transaction.BlockNumber.Value != 0))
            {
                return false;
            }

            return true;
        }

    }

    public class TansactionTrace
    {
        public int Gas { get; set; }
        public string ReturnValue { get; set; }
        public TransactionStructLog[] StructLogs { get; set; }
    }

    public class TransactionStructLog
    {
        public string Error { get; set; }
    }
}

﻿using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Settings;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Lykke.Service.EthereumCore.Core;
using Nethereum.Hex.HexTypes;
using System.Numerics;

namespace Lykke.Service.EthereumCore.Services
{
    public interface IEthereumTransactionService
    {
        Task<bool> IsTransactionInPool(string transactionHash);
        Task<bool> IsTransactionExecuted(string hash, int gasSended);
        Task<bool> IsTransactionExecuted(string hash, string gasForCoinTransaction);
        Task<TransactionReceipt> GetTransactionReceipt(string transaction);
    }

    public class EthereumTransactionService : IEthereumTransactionService
    {
        private readonly IBaseSettings _settings;
        private readonly HexBigInteger _failedStatus;
        private readonly Web3 _client;

        public EthereumTransactionService(IBaseSettings settings, Web3 client)
        {
            _client = client;
            _settings = settings;
            _failedStatus = new HexBigInteger(BigInteger.Zero);
        }

        // Do not use with private wallets
        public async Task<bool> IsTransactionExecuted(string hash, int gasSended)
        {
            var receipt = await GetTransactionReceipt(hash);

            if (receipt == null)
                return false;

            Transaction transaction = await _client.Eth.Transactions.GetTransactionByHash.SendRequestAsync(hash);

            if (receipt.Status != null && 
                receipt.Status.Value == _failedStatus.Value)
                return false;

            if (receipt.GasUsed.Value != transaction.Gas)
                return true;

            return false;
        }

        public async Task<bool> IsTransactionExecuted(string hash, string gasForCoinTransaction)
        {
            return await IsTransactionExecuted(hash, gasForCoinTransaction);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Settings;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace Services
{
    public interface IEthereumTransactionService
    {
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

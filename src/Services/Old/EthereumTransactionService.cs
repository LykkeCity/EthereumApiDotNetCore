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

		public EthereumTransactionService(IBaseSettings settings)
		{
			_settings = settings;
		}

		public async Task<bool> IsTransactionExecuted(string hash, int gasSended)
		{
			var receipt = await GetTransactionReceipt(hash);

			if (receipt == null)
				return false;

			if (receipt.GasUsed.Value != new Nethereum.Hex.HexTypes.HexBigInteger(gasSended).Value)
				return true;

			var web3 = new Web3(_settings.EthereumUrl);
			var logs =
				await
					web3.DebugGeth.TraceTransaction.SendRequestAsync(hash, new Nethereum.RPC.DebugGeth.DTOs.TraceTransactionOptions());

			var obj = logs.ToObject<TansactionTrace>();
			if (obj.StructLogs?.Length > 0 && !string.IsNullOrWhiteSpace(obj.StructLogs[obj.StructLogs.Length - 1].Error))
				return false;

			return true;
		}


		public async Task<TransactionReceipt> GetTransactionReceipt(string transaction)
		{
			var web3 = new Web3(_settings.EthereumUrl);
			return await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transaction);
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

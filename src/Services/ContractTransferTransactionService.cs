using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Log;
using Newtonsoft.Json;

namespace Services
{
	public class ContractTransferTransaction
	{
		public string TransactionHash { get; set; }

		public string Contract { get; set; }

		public decimal Amount { get; set; }

		public DateTime CreateDt { get; set; }
	}


	public interface IContractTransferTransactionService
	{
		Task PutContractTransferTransaction(ContractTransferTransaction tr);

		/// <summary>
		/// Returns true if transaction completed successfuly
		/// </summary>		
		Task<bool> CompleteTransaction();
	}

	public class ContractTransferTransactionService : IContractTransferTransactionService
	{
		private readonly IEthereumQueueOutService _queueOutService;
		private readonly IContractService _contractService;
		private readonly ILog _logger;
		private readonly IQueueExt _queue;

		public ContractTransferTransactionService(Func<string, IQueueExt> queueFactory, IEthereumQueueOutService queueOutService, 
			IContractService contractService, ILog logger)
		{
			_queueOutService = queueOutService;
			_contractService = contractService;
			_logger = logger;
			_queue = queueFactory(Constants.ContractTransferQueue);
		}

		public async Task PutContractTransferTransaction(ContractTransferTransaction tr)
		{
			await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(tr));
		}

		public async Task<bool> CompleteTransaction()
		{
			var item = await _queue.GetRawMessageAsync();

			if (item == null)
				return false;

			var contractTransferTr = JsonConvert.DeserializeObject<ContractTransferTransaction>(item.AsString);

			if (_contractService.GetTransactionReceipt(contractTransferTr.TransactionHash) != null)
			{
				await _queueOutService.FirePaymentEvent(contractTransferTr.Contract, contractTransferTr.Amount,
					contractTransferTr.TransactionHash);
				await _logger.WriteInfo("EthereumJob", "CompleteTransaction", "", $"Message sended to ethereum-queue-out : Event from {contractTransferTr.Contract}. Transaction: {contractTransferTr.TransactionHash}");
				await _queue.FinishRawMessageAsync(item);
				return true;
			}
			return false;
		}
	}
}

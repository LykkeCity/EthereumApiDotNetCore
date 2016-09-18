using System;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Log;
using Newtonsoft.Json;

namespace Services
{
	public interface IEthereumQueueOutService
	{
		Task FirePaymentEvent(string userContract, decimal amount);
	}

	public class EthereumQueueOutService : IEthereumQueueOutService
	{
		private readonly IQueueExt _queue;
		private readonly ILog _logger;
		
		public EthereumQueueOutService(Func<string, IQueueExt> queueFactory, ILog logger)
		{
			_queue = queueFactory(Constants.EthereumOutQueue);
			_logger = logger;
		}

		/// <summary>
		/// Sends event for ethereum poayment to azure queue
		/// </summary>
		/// <param name="userContract"></param>
		/// <param name="amount"></param>
		/// <returns></returns>
		public async Task FirePaymentEvent(string userContract, decimal amount)
		{
			try
			{
				var model = new EthereumCashInModel
				{
					Amount = amount,
					Contract = userContract
				};

				var json = JsonConvert.SerializeObject(model);

				await _queue.PutRawMessageAsync(json);
			}
			catch (Exception e)
			{
				await _logger.WriteError("ApiCallService", "FirePaymentEvent", $"Contract : {userContract}, amount: {amount}", e);
			}
		}
	}

	public class EthereumCashInModel
	{
		public string Type => "EthereumCashIn";
		public decimal Amount { get; set; }
		public string Contract { get; set; }
	}
}

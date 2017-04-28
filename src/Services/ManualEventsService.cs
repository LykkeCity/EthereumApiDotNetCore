using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Newtonsoft.Json;
using Common.Log;
using AzureStorage.Queue;

namespace Services
{
	public interface IManualEventsService
	{
		Task<bool> ProcessManualEvent();
	}
	///TODO: Write tests
	public class ManualEventsService : IManualEventsService
	{
		private readonly ILog _logger;
		private readonly IPaymentService _paymentService;
		private readonly IQueueExt _queue;

		public ManualEventsService(ILog logger, Func<string, IQueueExt> queueFactory, IPaymentService paymentService)
		{
			_logger = logger;
			_paymentService = paymentService;
			_queue = queueFactory(Constants.UserContractManualQueue);
		}

		public async Task<bool> ProcessManualEvent()
		{
			var item = await _queue.GetRawMessageAsync();

			if (item == null)
				return false;

			var obj = JsonConvert.DeserializeObject<ManualTransaction>(item.AsString);

			var trHash = await _paymentService.TransferFromUserContract(obj.Address, obj.Amount);

			await _logger.WriteInfoAsync("ManualEventsService", "ProcessItem", "", $"Transfer manual payment from {obj.Address}, amount: {obj.Amount} ETH, hash: {trHash}");

			await _queue.FinishRawMessageAsync(item);

			return true;
		}
	}

	public class ManualTransaction
	{
		public string Address { get; set; }
		public decimal Amount { get; set; }
	}
}

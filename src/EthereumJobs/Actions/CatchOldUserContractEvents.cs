using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Repositories;
using EthereumJobs.Job;
using Services;
using Common.Log;

namespace EthereumJobs.Actions
{
    public class CatchOldUserContractEvents
    {
		private readonly ILog _logger;
	    private readonly IContractService _contractService;
	    private readonly IPaymentService _paymentService;

		public CatchOldUserContractEvents(ILog logger, IContractService contractService, IPaymentService paymentService)
		{
			_logger = logger;
			_contractService = contractService;
			_paymentService = paymentService;
		}

		public async Task Start()
		{
			try
			{
				Console.WriteLine($"Checking old events (fired when job was offline)");

				var filter = await _contractService.GetFilterEventForUserContractPayment();

				var logs = await _contractService.GetNewPaymentEvents(filter);

				// recreate filter (we dont know about Ethereum node, if it was offline our old filter was deleted)
				await _contractService.CreateFilterEventForUserContractPayment();

				foreach (var log in logs)
					await _paymentService.ProcessPaymentEvent(log);

				Console.WriteLine("Checking finished!");
			}
			catch (Exception e)
			{
				await _logger.WriteErrorAsync("CatchOldUserContractEvents", "Start", "", e);
			}
		}
	}
}

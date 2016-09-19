using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Log;
using Core.Repositories;
using EthereumJobs.Job;
using Services;

namespace EthereumJobs.Actions
{
    public class CatchOldUserContractEvents
    {
		private readonly ILog _logger;
	    private readonly IContractService _contractService;
	    private readonly CheckPaymentsToUserContractsJob _job;

		public CatchOldUserContractEvents(ILog logger, IContractService contractService, CheckPaymentsToUserContractsJob job)
		{
			_logger = logger;
			_contractService = contractService;
			_job = job;
		}

		public async Task Start()
		{
			try
			{
				Console.WriteLine($"Checking old events (fired when job was offline)");

				var filter = await _contractService.GetFilterEventForUserContractPayment();

				var logs = await _contractService.GetNewPaymentEvents(filter);

				foreach (var log in logs)
					await _job.ProcessLogItem(log);

				// recreate filter (we dont know about Ethereum node, if it was offline our old filter was deleted)
				await _contractService.CreateFilterEventForUserContractPayment();

				Console.WriteLine("Checking finished!");
			}
			catch (Exception e)
			{
				await _logger.WriteError("CatchOldUserContractEvents", "Start", "", e);
			}
		}
	}
}

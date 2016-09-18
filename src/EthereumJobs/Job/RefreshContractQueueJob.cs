using System;
using System.Threading.Tasks;
using Core.Log;
using Core.Timers;
using Services;

namespace EthereumJobs.Job
{
	public class RefreshContractQueueJob : TimerPeriod
	{
		private const int TimerPeriodSeconds = 12 * 60 * 60; // 12 hours

		private readonly IContractQueueService _contractQueueService;
		private readonly ILog _logger;

		public RefreshContractQueueJob(IContractQueueService contractQueueService, ILog logger)
			: this("RefreshContractQueueJob", TimerPeriodSeconds * 1000, logger)
		{
			_contractQueueService = contractQueueService;
			_logger = logger;
		}

		private RefreshContractQueueJob(string componentName, int periodMs, ILog log) : base(componentName, periodMs, log)
		{
		}

		public override async Task Execute()
		{
			var count = await _contractQueueService.Count();
			for (var i = 0; i < count; i++)
			{
				try
				{
					var contract = await _contractQueueService.GetContract();
					await _contractQueueService.PushContract(contract);
				}
				catch (Exception e)
				{
					await _logger.WriteError("EthereumWebJob", "RefreshQueue", "", e);
				}
			}
		}
	}
}

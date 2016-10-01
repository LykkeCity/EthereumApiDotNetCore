using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Log;
using Core.Timers;
using Services.Coins;

namespace EthereumJobs.Job
{
	public class MonitoringCoinTransactionJob : TimerPeriod
	{
		private readonly ILog _log;
		private readonly ICoinTransactionService _coinTransactionService;
		private const int TimerPeriodSeconds = 2;
		public MonitoringCoinTransactionJob(ILog log, ICoinTransactionService coinTransactionService) :
			base("MonitoringCoinTransactionJob", TimerPeriodSeconds * 1000, log)
		{
			_log = log;
			_coinTransactionService = coinTransactionService;
		}

		public override async Task Execute()
		{
			try
			{
				while (await _coinTransactionService.ProcessTransaction() && Working)
				{
				}
			}
			catch (Exception ex)
			{
				await _log.WriteError("EthereumJob", "MonitoringCoinTransactionJob", "", ex);
			}
		}
	}
}

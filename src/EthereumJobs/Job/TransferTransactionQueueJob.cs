using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Log;
using Core.Timers;
using Services;

namespace EthereumJobs.Job
{
	public class TransferTransactionQueueJob : TimerPeriod
	{
		private readonly IContractTransferTransactionService _contractTransferTransactionService;
		private readonly ILog _log;
		private const int TimerPeriodSeconds = 2;


		public TransferTransactionQueueJob(IContractTransferTransactionService contractTransferTransactionService, ILog log) : base("TransferTransactionQueueJob", TimerPeriodSeconds, log)
		{
			_contractTransferTransactionService = contractTransferTransactionService;
			_log = log;
		}

		public override async Task Execute()
		{
			try
			{
				while (await _contractTransferTransactionService.CompleteTransaction() && Working)
				{
				}
			}
			catch (Exception ex)
			{
				_log.WriteError("EthereumWebJob", "TransferTransactionQueue", "", ex);
			}
		}
	}
}

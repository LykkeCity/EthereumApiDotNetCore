using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Log;
using Core.Repositories;
using Core.Timers;
using Services;

namespace EthereumJobs.Job
{
	public class MonitoringContractBalance : TimerPeriod
	{

		private const int TimerPeriodSeconds = 60 * 30;
		private const int AlertNotChangedBalanceCount = 3;

		private readonly IUserContractRepository _userContractRepository;
		private readonly ILog _logger;
		private readonly IPaymentService _paymentService;


		public MonitoringContractBalance(IUserContractRepository userContractRepository, ILog logger,
			IPaymentService paymentService) :
			base("MonitoringContractBalance", TimerPeriodSeconds, logger)
		{
			_userContractRepository = userContractRepository;
			_logger = logger;
			_paymentService = paymentService;
		}

		public override async Task Execute()
		{
			var contracts = (await _userContractRepository.GetContractsAsync()).ToList();
			foreach (var userContract in contracts)
			{
				var balance = await _paymentService.GetUserContractBalance(userContract.Address);
				if (balance == 0 && userContract.LastBalance == 0)
					continue;
				if (balance == 0 && userContract.LastBalance != 0)
				{
					userContract.LastBalance = 0;
					userContract.BalanceNotChangedCount = 0;
					await _userContractRepository.ReplaceAsync(userContract);
					continue;
				}
				if (balance != 0)
				{
					if (userContract.BalanceNotChangedCount == AlertNotChangedBalanceCount && balance == userContract.LastBalance)
					{
						//TODO: send alert
					}
					userContract.BalanceNotChangedCount = userContract.LastBalance == balance ? userContract.BalanceNotChangedCount++ : 0;
					userContract.LastBalance = balance;

					await _userContractRepository.ReplaceAsync(userContract);
				}

			}

		}
	}
}

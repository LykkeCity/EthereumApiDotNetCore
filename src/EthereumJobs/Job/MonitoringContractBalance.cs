using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Log;
using Core.Repositories;
using Core.Timers;
using Nethereum.Web3;
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
		private readonly IEmailNotifierService _emailNotifierService;


		public MonitoringContractBalance(IUserContractRepository userContractRepository, ILog logger,
			IPaymentService paymentService, IEmailNotifierService emailNotifierService) :
			base("MonitoringContractBalance", TimerPeriodSeconds * 1000, logger)
		{
			_userContractRepository = userContractRepository;
			_logger = logger;
			_paymentService = paymentService;
			_emailNotifierService = emailNotifierService;
		}

		public override async Task Execute()
		{
			await _userContractRepository.ProcessContractsAsync(async contracts =>
			{
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
							await
								_paymentService.ProcessPaymentEvent(new Core.ContractEvents.UserPaymentEvent
								{
									Address = userContract.Address,
									Amount = UnitConversion.Convert.ToWei(balance)
								});
							_emailNotifierService.Warning("User contract balance is freezed",
								$"User contract {userContract.Address} has constant amount of {userContract.LastBalance} ETH");
						}
						userContract.BalanceNotChangedCount = userContract.LastBalance == balance
							? userContract.BalanceNotChangedCount + 1
							: 0;
						userContract.LastBalance = balance;

						await _logger.WriteWarning("MonitoringContractBalance", "Execute", "", $"User contract {userContract.Address} has {balance} ETH in {userContract.BalanceNotChangedCount} of 3 check");

						await _userContractRepository.ReplaceAsync(userContract);						
					}
				}
			});

		}
	}
}

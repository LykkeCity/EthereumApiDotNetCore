using System;
using System.Threading.Tasks;
using Core.Log;
using Core.Settings;
using Core.Timers;
using Services;

namespace EthereumJobs.Job
{
	public class CheckContractQueueCountJob : TimerPeriod
	{
		private const int TimerPeriodSeconds = 5;

		private const int ContractsPerRequest = 50;

		private readonly IContractQueueService _contractQueueService;
		private readonly IContractService _contractService;
		private readonly IBaseSettings _settings;
		private readonly IPaymentService _paymentService;
		private readonly IEmailNotifierService _emailNotifier;
		private readonly ILog _logger;

		private bool _balanceWarningSended;

		public CheckContractQueueCountJob(IContractQueueService contractQueueService, IContractService contractService, IBaseSettings settings, IPaymentService paymentService, IEmailNotifierService emailNotifier, ILog logger)
			: this("CheckContractQueueCountJob", TimerPeriodSeconds * 1000, logger)
		{
			_contractQueueService = contractQueueService;
			_contractService = contractService;
			_settings = settings;
			_paymentService = paymentService;
			_emailNotifier = emailNotifier;
			_logger = logger;
		}

		private CheckContractQueueCountJob(string componentName, int periodMs, ILog log)
			: base(componentName, periodMs, log)
		{
		}

		public override async Task Execute()
		{
			await InternalBalanceCheck();

			if (await _contractQueueService.Count() < _settings.MinContractPoolLength)
			{
				for (var i = 0; i < (_settings.MaxContractPoolLength - _settings.MinContractPoolLength) / ContractsPerRequest; i++)
				{
					await InternalBalanceCheck();

					var contracts = await _contractService.GenerateUserContracts(ContractsPerRequest);
					foreach (var contract in contracts)
						await _contractQueueService.PushContract(contract);
				}
			}
		}

		private async Task InternalBalanceCheck()
		{
			try
			{
				var balance = await _paymentService.GetMainAccountBalance();
				if (balance < _settings.MainAccountMinBalance)
				{
					string message =
						$"Main account {_settings.EthereumMainAccount} balance is less that {_settings.MainAccountMinBalance} ETH !";
					await _logger.WriteWarning("EthereumWebJob", "InternalBalanceCheck", "", message);

					if (!_balanceWarningSended)
						_emailNotifier.Warning("Ethereum worker", message);

					_balanceWarningSended = true;
				}
				else
				{
					// reset if balance become higher
					_balanceWarningSended = false;
				}
			}
			catch (Exception e)
			{
				await _logger.WriteError("EthereumWebJob", "InternalBalanceCheck", "", e);
			}
		}
	}
}

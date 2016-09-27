using System;
using System.Threading.Tasks;
using Core.ContractEvents;
using Core.Log;
using Core.Timers;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Services;

namespace EthereumJobs.Job
{
	public class CheckPaymentsToUserContractsJob : TimerPeriod
	{
		private const int TimerPeriodSeconds = 5;

		private readonly IContractService _contractService;
		private readonly IPaymentService _paymentService;
		private readonly IContractTransferTransactionService _contractTransferTransactionService;
		private readonly ILog _logger;

		private bool _shouldCreateNewEvent;
		private HexBigInteger _filter;

		public CheckPaymentsToUserContractsJob(IContractService contractService, IPaymentService paymentService,
												IContractTransferTransactionService contractTransferTransactionService, ILog logger)
			: this("CheckPaymentsToUserContractsJob", TimerPeriodSeconds * 1000, logger)
		{
			_contractService = contractService;
			_paymentService = paymentService;
			_contractTransferTransactionService = contractTransferTransactionService;
			_logger = logger;
		}

		private CheckPaymentsToUserContractsJob(string componentName, int periodMs, ILog log)
			: base(componentName, periodMs, log)
		{
		}

		public override async Task Execute()
		{
			try
			{
				if (_shouldCreateNewEvent)
				{
					_filter = await _contractService.CreateFilterEventForUserContractPayment();
					_shouldCreateNewEvent = false;
				}

				if (_filter == null)
					_filter = await _contractService.GetFilterEventForUserContractPayment();

				var logs = await _contractService.GetNewPaymentEvents(_filter);

				if (logs == null)
					return;

				foreach (var item in logs)
				{
					await _paymentService.ProcessPaymentEvent(item);
				}
			}
			catch (Exception e)
			{
				// ethereum, node is down
				if (e.Message.Contains("when trying to send rpc"))
					_shouldCreateNewEvent = true;
				throw;
			}
		}
	}
}

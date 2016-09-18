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
		private readonly IEthereumQueueOutService _queueOutService;
		private readonly ILog _logger;

		private HexBigInteger _filter;

		public CheckPaymentsToUserContractsJob(IContractService contractService, IPaymentService paymentService,
												IEthereumQueueOutService queueOutService, ILog logger)
			: this("CheckPaymentsToUserContractsJob", TimerPeriodSeconds * 1000, logger)
		{
			_contractService = contractService;
			_paymentService = paymentService;
			_queueOutService = queueOutService;
			_logger = logger;
		}

		private CheckPaymentsToUserContractsJob(string componentName, int periodMs, ILog log)
			: base(componentName, periodMs, log)
		{
		}

		public override async Task Execute()
		{
			if (_filter == null)
				_filter = await _contractService.CreateFilterEventForUserContractPayment();

			var logs = await _contractService.GetNewPaymentEvents(_filter);

			if (logs == null)
				return;

			foreach (var item in logs)
			{
				await ProcessLogItem(item);
			}
		}

		/// <summary>
		/// Process one payment event. Try to transfer from contract to main account (if failed, then it is duplicated event)
		/// </summary>
		/// <param name="log"></param>
		/// <returns></returns>
		private async Task<bool> ProcessLogItem(UserPaymentEvent log)
		{
			try
			{
				await _logger.WriteInfo("EthereumWebJob", "ProcessLogItem", "", $"Start proces: event from {log.Address} for {log.Amount} WEI.");

				var transaction = await _paymentService.TransferFromUserContract(log.Address, log.Amount);

				await _logger.WriteInfo("EthereumWebJob", "ProcessLogItem", "", $"Finish process: Event from {log.Address} for {log.Amount} WEI. Transaction: {transaction}");

				await _queueOutService.FirePaymentEvent(log.Address, UnitConversion.Convert.FromWei(log.Amount));

				await _logger.WriteInfo("EthereumWebJob", "ProcessLogItem", "", $"Message sended to queue: Event from {log.Address}");

				Console.WriteLine($"Event from {log.Address} for {log.Amount} WEI processed! Transaction: {transaction}");

				return true;
			}
			catch (Exception e)
			{
				await _logger.WriteError("EthereumWebJob", "ProcessLogItem", "Failed to process item", e);
			}

			return false;
		}
	}
}

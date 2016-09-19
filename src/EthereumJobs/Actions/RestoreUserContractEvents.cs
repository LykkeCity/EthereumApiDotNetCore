using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Log;
using Core.Repositories;
using Services;

namespace EthereumJobs.Actions
{
	public class RestoreUserContractEvents
	{
		private readonly ILog _logger;
		private readonly IPaymentService _paymentService;
		private readonly IEthereumQueueOutService _queueOutService;
		private readonly IUserContractRepository _userContractRepository;

		public RestoreUserContractEvents(ILog logger, IPaymentService paymentService, IEthereumQueueOutService queueOutService, IUserContractRepository userContractRepository)
		{
			_logger = logger;
			_paymentService = paymentService;
			_queueOutService = queueOutService;
			_userContractRepository = userContractRepository;
		}

		public async Task Start()
		{
			try
			{
				var contracts = (await _userContractRepository.GetContractsAsync()).ToList();

				Console.WriteLine($"Checking contracts, total count: {contracts.Count} (will log after each 50 processed)");

				for (var i = 0; i < contracts.Count; i++)
				{
					await ProcessItem(contracts[i]);
					if (i > 0 && i % 50 == 0)
						Console.WriteLine($"Processed {i} of {contracts.Count}");
				}

				Console.WriteLine("Checking finished!");
			}
			catch (Exception e)
			{
				await _logger.WriteError("RestoreUserContractEvents", "Start", "", e);
			}
		}

		private async Task ProcessItem(IUserContract contract)
		{
			try
			{
				var balance = await _paymentService.GetUserContractBalance(contract.Address);
				if (balance == 0)
					return;

				await _logger.WriteInfo("RestoreUserContractEvents", "ProcessItem", "", $"Restoing payment event, address {contract.Address}, balance: {balance}");

				var transaction = await _paymentService.TransferFromUserContract(contract.Address, balance);

				await _queueOutService.FirePaymentEvent(contract.Address, balance);

				await _logger.WriteInfo("EthereumWebJob", "ProcessLogItem", "", $"Message sended to queue: Event from {contract.Address}. Transaction: {transaction}");

			}
			catch (Exception e)
			{
				await _logger.WriteError("RestoreUserContractEvents", "ProcessItem", contract.Address, e);
			}
		}
	}
}

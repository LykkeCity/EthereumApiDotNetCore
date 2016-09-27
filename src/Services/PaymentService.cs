using System;
using System.Numerics;
using System.Threading.Tasks;
using Core.ContractEvents;
using Core.Log;
using Core.Settings;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace Services
{
	public interface IPaymentService
	{
		/// <summary>
		/// Transfer money from user contract to main account
		/// </summary>
		/// <param name="contractAddress">Address of user contract</param>
		/// <param name="amount">WEI amount</param>
		/// <returns></returns>
		Task<string> TransferFromUserContract(string contractAddress, BigInteger amount);

		/// <summary>
		/// Transfer money from user contract to main account
		/// </summary>
		/// <param name="contractAddress">Address of user contract</param>
		/// <param name="amount">ETH amount</param>
		/// <returns></returns>
		Task<string> TransferFromUserContract(string contractAddress, decimal amount);

		Task<decimal> GetMainAccountBalance();

		Task<decimal> GetUserContractBalance(string address);

		Task<bool> ProcessPaymentEvent(UserPaymentEvent log);
	}

	public class PaymentService : IPaymentService
	{
		private readonly IBaseSettings _settings;
		private readonly ILog _logger;
		private readonly IContractTransferTransactionService _contractTransferTransactionService;

		public PaymentService(IBaseSettings settings, ILog logger, IContractTransferTransactionService contractTransferTransactionService)
		{
			_settings = settings;
			_logger = logger;
			_contractTransferTransactionService = contractTransferTransactionService;
		}

		public async Task<string> TransferFromUserContract(string contractAddress, decimal amount)
		{
			return await TransferFromUserContract(contractAddress, UnitConversion.Convert.ToWei(amount));
		}

		public async Task<string> TransferFromUserContract(string contractAddress, BigInteger amount)
		{
			var web3 = new Web3(_settings.EthereumUrl);

			// unlock account for 120 seconds
			await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount, _settings.EthereumMainAccountPassword, new HexBigInteger(120));

			var balance = await web3.Eth.GetBalance.SendRequestAsync(contractAddress);

			if (balance < amount)
				throw new Exception($"TransferFromUserContract failed, contract balance is {balance}, amount is {amount}");

			var contract = web3.Eth.GetContract(_settings.UserContract.Abi, contractAddress);

			var function = contract.GetFunction("transferMoney");

			return await function.SendTransactionAsync(_settings.EthereumMainAccount, _settings.EthereumPrivateAccount, amount);
		}

		public async Task<decimal> GetMainAccountBalance()
		{
			var web3 = new Web3(_settings.EthereumUrl);
			var balance = await web3.Eth.GetBalance.SendRequestAsync(_settings.EthereumMainAccount);
			return UnitConversion.Convert.FromWei(balance);
		}

		public async Task<decimal> GetUserContractBalance(string address)
		{
			var web3 = new Web3(_settings.EthereumUrl);
			var balance = await web3.Eth.GetBalance.SendRequestAsync(address);
			return UnitConversion.Convert.FromWei(balance);
		}

		public async Task<bool> ProcessPaymentEvent(UserPaymentEvent log)
		{
			try
			{
				await _logger.WriteInfo("EthereumJob", "ProcessPaymentEvent", "", $"Start proces: event from {log.Address} for {log.Amount} WEI.");

				var transaction = await TransferFromUserContract(log.Address, log.Amount);

				await _logger.WriteInfo("EthereumJob", "ProcessPaymentEvent", "", $"Finish process: Event from {log.Address} for {log.Amount} WEI. Transaction: {transaction}");

				await _contractTransferTransactionService.PutContractTransferTransaction(new ContractTransferTransaction
				{
					TransactionHash = transaction,
					Contract = log.Address,
					Amount = UnitConversion.Convert.FromWei(log.Amount),
					CreateDt = DateTime.UtcNow
				});

				await _logger.WriteInfo("EthereumJob", "ProcessPaymentEvent", "", $"Message sended to queue: Event from {log.Address}. Transaction: {transaction}");

				return true;
			}
			catch (Exception e)
			{
				await _logger.WriteError("EthereumJob", "ProcessPaymentEvent", "Failed to process item", e);
			}

			return false;
		}
	}
}

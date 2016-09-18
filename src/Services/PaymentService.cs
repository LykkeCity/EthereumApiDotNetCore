using System;
using System.Numerics;
using System.Threading.Tasks;
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

		Task<decimal> GetMainAccountBalance();
	}

	public class PaymentService : IPaymentService
	{
		private readonly IBaseSettings _settings;

		public PaymentService(IBaseSettings settings)
		{
			_settings = settings;
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

			var transaction = await function.SendTransactionAsync(_settings.EthereumMainAccount, _settings.EthereumPrivateAccount, amount);

			TransactionReceipt receipt;

			while ((receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transaction)) == null)
			{
				await Task.Delay(100);
			}

			return receipt.TransactionHash;
		}

		public async Task<decimal> GetMainAccountBalance()
		{
			var web3 = new Web3(_settings.EthereumUrl);
			var balance = await web3.Eth.GetBalance.SendRequestAsync(_settings.EthereumMainAccount);
			return UnitConversion.Convert.FromWei(balance);
		}
	}
}

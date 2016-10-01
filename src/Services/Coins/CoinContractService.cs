using System.Threading.Tasks;
using Core;
using Core.Settings;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;

namespace Services.Coins
{

	

	public interface ICoinContractService
	{
		Task<string> Swap(string clientA, string clientB, string coinA, string coinB, decimal amountA, decimal amountB,
			string signAHex, string signBHex);

		Task<string> CashIn(string receiver, decimal amount);

		Task<string> CashOut(string coinAddr, string clientAddr, string toAddr, decimal amount, string sign);

		Task PingMainExchangeContract();

	}

	public class CoinContractService : ICoinContractService
	{
		private readonly IBaseSettings _settings;
		private readonly ICoinTransactionService _cointTransactionService;


		public CoinContractService(IBaseSettings settings, ICoinTransactionService cointTransactionService)
		{
			_settings = settings;
			_cointTransactionService = cointTransactionService;
		}


		public async Task<string> Swap(string clientA, string clientB, string coinA, string coinB, decimal amountA, decimal amountB, string signAHex,
			string signBHex)
		{
			var web3 = new Web3(_settings.EthereumUrl);

			await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount, _settings.EthereumMainAccountPassword, new HexBigInteger(120));

			var contract = web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.EthereumMainExchangeContractAddress);

			var swap = contract.GetFunction("swap");
			var tr = await swap.SendTransactionAsync(_settings.EthereumMainAccount, new HexBigInteger(Constants.GasForUserContractTransafer),
						new HexBigInteger(0), clientA, clientB, coinA, coinB, UnitConversion.Convert.ToWei(amountA), UnitConversion.Convert.ToWei(amountB),
						signAHex, signBHex);
			await _cointTransactionService.PutTransactionToQueue(tr);
			return tr;
		}

		public async Task<string> CashIn(string receiver, decimal amount)
		{
			var web3 = new Web3(_settings.EthereumUrl);

			await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount, _settings.EthereumMainAccountPassword, new HexBigInteger(120));

			var contract = web3.Eth.GetContract(_settings.EthCoinContract.Abi, _settings.EthereumEthCoinContract);

			var cashin = contract.GetFunction("cashin");
			var tr = await cashin.SendTransactionAsync(_settings.EthereumMainAccount, new HexBigInteger(Constants.GasForUserContractTransafer),
						new HexBigInteger(UnitConversion.Convert.ToWei(amount)), receiver, new HexBigInteger(0));
			await _cointTransactionService.PutTransactionToQueue(tr);
			return tr;
		}

		public async Task<string> CashOut(string coinAddr, string clientAddr, string toAddr, decimal amount, string sign)
		{
			var web3 = new Web3(_settings.EthereumUrl);

			await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount, _settings.EthereumMainAccountPassword, new HexBigInteger(120));

			var contract = web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.EthereumMainExchangeContractAddress);
			var cashout = contract.GetFunction("cashout");
			var tr = await cashout.SendTransactionAsync(_settings.EthereumMainAccount,
						new HexBigInteger(Constants.GasForUserContractTransafer), new HexBigInteger(0),
						coinAddr, clientAddr, toAddr, UnitConversion.Convert.ToWei(amount), sign);
			await _cointTransactionService.PutTransactionToQueue(tr);
			return tr;

		}

		public async Task PingMainExchangeContract()
		{
			var web3 = new Web3(_settings.EthereumUrl);
			await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount, _settings.EthereumMainAccountPassword, new HexBigInteger(120));
			var contract = web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.EthereumMainExchangeContractAddress);
			var ping = contract.GetFunction("ping");
			await ping.SendTransactionAsync(_settings.EthereumMainContractAddress);
		}		
	}
}

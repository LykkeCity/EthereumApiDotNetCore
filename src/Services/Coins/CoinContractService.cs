using System;
using System.Numerics;
using System.Threading.Tasks;
using Core;
using Core.Settings;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Nethereum.Hex.HexConvertors.Extensions;
using Core.Utils;

namespace Services.Coins
{



	public interface ICoinContractService
	{
		Task<string> Swap(Guid id, string clientA, string clientB, string coinA, string coinB, decimal amountA, decimal amountB,
			string signAHex, string signBHex);

		Task<string> CashIn(string coinAddr, string receiver, decimal amount, bool ethCoin = false);

		Task<string> CashOut(Guid id, string coinAddr, string clientAddr, string toAddr, decimal amount, string sign);

		Task<BigInteger> GetBalance(string coinAddr, string clientAddr);

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


		public async Task<string> Swap(Guid id, string clientA, string clientB, string coinA, string coinB, decimal amountA, decimal amountB, string signAHex,
			string signBHex)
		{
			var web3 = new Web3(_settings.EthereumUrl);

			await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount, _settings.EthereumMainAccountPassword, new HexBigInteger(120));

			var contract = web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.EthereumMainExchangeContractAddress);

			var coinContractA = _settings.CoinContracts[coinA];
			var convertedAmountA = coinContractA.GetInternalValue(amountA);

			var coinContractB = _settings.CoinContracts[coinB];
			var convertedAmountB = coinContractB.GetInternalValue(amountB);

			var convertedId = new BigInteger(id.ToByteArray());

			var swap = contract.GetFunction("swap");
			var tr = await swap.SendTransactionAsync(_settings.EthereumMainAccount, new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0),
					convertedId, clientA, clientB, coinA, coinB, convertedAmountA, convertedAmountB, signAHex.HexToByteArray(), signBHex.HexToByteArray());
			await _cointTransactionService.PutTransactionToQueue(tr);
			return tr;
		}

		public async Task<string> CashIn(string coinAddr, string receiver, decimal amount, bool ethCoin = false)
		{
			var web3 = new Web3(_settings.EthereumUrl);

			await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount, _settings.EthereumMainAccountPassword, new HexBigInteger(120));

			var contract = web3.Eth.GetContract(_settings.CoinContracts[coinAddr].Abi, coinAddr);

			var convertedAmountA = BigInteger.Parse(_settings.CoinContracts[coinAddr].Multiplier ?? "1") * new BigInteger(amount);

			var cashin = contract.GetFunction("cashin");
			string tr;
			if (ethCoin)
			{
				tr = await cashin.SendTransactionAsync(_settings.EthereumMainAccount, new HexBigInteger(Constants.GasForCoinTransaction),
							new HexBigInteger(convertedAmountA), receiver, 0);
			}
			else
			{
				tr = await cashin.SendTransactionAsync(_settings.EthereumMainAccount, new HexBigInteger(Constants.GasForCoinTransaction),
							new HexBigInteger(0), receiver, convertedAmountA);
			}
			await _cointTransactionService.PutTransactionToQueue(tr);
			return tr;
		}

		public async Task<string> CashOut(Guid id, string coinAddr, string clientAddr, string toAddr, decimal amount, string sign)
		{
			var web3 = new Web3(_settings.EthereumUrl);

			await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount, _settings.EthereumMainAccountPassword, new HexBigInteger(120));

			var coinContract = _settings.CoinContracts[coinAddr];
			var convertedAmount = coinContract.GetInternalValue(amount);

			var contract = web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.EthereumMainExchangeContractAddress);
			var cashout = contract.GetFunction("cashout");

			var convertedId = new BigInteger(id.ToByteArray());

			var tr = await cashout.SendTransactionAsync(_settings.EthereumMainAccount,
						new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0),
						convertedId, coinAddr, clientAddr, toAddr, convertedAmount, sign.HexToByteArray());
			await _cointTransactionService.PutTransactionToQueue(tr);
			return tr;

		}

		public async Task<BigInteger> GetBalance(string coinAddr, string clientAddr)
		{
			var web3 = new Web3(_settings.EthereumUrl);

			var contract = web3.Eth.GetContract(_settings.CoinContracts[coinAddr].Abi, coinAddr);

			var cashin = contract.GetFunction("coinBalanceMultisig");
			return await cashin.CallAsync<BigInteger>(clientAddr);
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

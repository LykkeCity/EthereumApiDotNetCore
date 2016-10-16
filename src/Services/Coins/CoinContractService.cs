using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Repositories;
using Core.Settings;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Nethereum.Hex.HexConvertors.Extensions;
using Core.Utils;
using Newtonsoft.Json;
using Services.Coins.Models;
using Services.Coins.Models.Events;

namespace Services.Coins
{



	public interface ICoinContractService
	{
		Task<string> Swap(Guid id, string clientA, string clientB, string coinA, string coinB, decimal amountA, decimal amountB,
			string signAHex, string signBHex);

		Task<string> CashIn(Guid id, string coinAddr, string receiver, decimal amount, bool ethCoin = false);

		Task<string> CashOut(Guid id, string coinAddr, string clientAddr, string toAddr, decimal amount, string sign);

		Task<BigInteger> GetBalance(string coinAddr, string clientAddr);

		Task PingMainExchangeContract();

		Task<IEnumerable<ICoinContractFilter>> GetCoinContractFilters(bool recreate);

		Task RetrieveEventLogs(bool recreateFilters);
	}

	public class CoinContractService : ICoinContractService
	{
		private readonly IBaseSettings _settings;
		private readonly ICoinTransactionService _cointTransactionService;
		private readonly IContractService _contractService;
		private readonly ICoinContractFilterRepository _coinContractFilterRepository;
		private IQueueExt _coinEventQueue;


		public CoinContractService(IBaseSettings settings,
			ICoinTransactionService cointTransactionService, IContractService contractService,
			ICoinContractFilterRepository coinContractFilterRepository, Func<string, IQueueExt> queueFactory)
		{
			_settings = settings;
			_cointTransactionService = cointTransactionService;
			_contractService = contractService;
			_coinContractFilterRepository = coinContractFilterRepository;
			_coinEventQueue = queueFactory(Constants.CoinEventQueue);
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

			var convertedId = EthUtils.GuidToBigInteger(id);

			var swap = contract.GetFunction("swap");
			var tr = await swap.SendTransactionAsync(_settings.EthereumMainAccount, new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0),
					convertedId, clientA, clientB, coinA, coinB, convertedAmountA, convertedAmountB, signAHex.HexToByteArray(), signBHex.HexToByteArray());
			await _cointTransactionService.PutTransactionToQueue(tr);
			return tr;
		}

		public async Task<string> CashIn(Guid id, string coinAddr, string receiver, decimal amount, bool ethCoin = false)
		{
			var web3 = new Web3(_settings.EthereumUrl);

			await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount, _settings.EthereumMainAccountPassword, new HexBigInteger(120));

			var contract = web3.Eth.GetContract(_settings.CoinContracts[coinAddr].Abi, coinAddr);

			var convertedAmountA = _settings.CoinContracts[coinAddr].GetInternalValue(amount);
			var convertedId = EthUtils.GuidToBigInteger(id);

			var cashin = contract.GetFunction("cashin");
			string tr;
			if (ethCoin)
			{
				tr = await cashin.SendTransactionAsync(_settings.EthereumMainAccount, new HexBigInteger(Constants.GasForCoinTransaction),
							new HexBigInteger(convertedAmountA), convertedId, receiver, 0);
			}
			else
			{
				tr = await cashin.SendTransactionAsync(_settings.EthereumMainAccount, new HexBigInteger(Constants.GasForCoinTransaction),
							new HexBigInteger(0), convertedId, receiver, convertedAmountA);
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

			var convertedId = EthUtils.GuidToBigInteger(id);

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
			await ping.SendTransactionAsync(_settings.EthereumMainAccount);
		}

		public async Task<IEnumerable<ICoinContractFilter>> GetCoinContractFilters(bool recreate)
		{
			var result = new List<ICoinContractFilter>();

			if (recreate)
				await _coinContractFilterRepository.Clear();
			var savedFilters = (await _coinContractFilterRepository.GetListAsync()).GroupBy(o => o.ContractAddress).ToDictionary(o => o.Key, o => o);
			foreach (var settingsCoinContract in _settings.CoinContracts)
			{
				var filtersOfContract = savedFilters.ContainsKey(settingsCoinContract.Key) ? savedFilters[settingsCoinContract.Key] : null;
				result.Add(await GetOrCreateEvent(settingsCoinContract, filtersOfContract, Constants.CashInEvent));
				result.Add(await GetOrCreateEvent(settingsCoinContract, filtersOfContract, Constants.CashOutEvent));
				result.Add(await GetOrCreateEvent(settingsCoinContract, filtersOfContract, Constants.TransferEvent));
			}
			return result;
		}

		private async Task<ICoinContractFilter> GetOrCreateEvent(KeyValuePair<string, EthereumContract> settingsCoinContract, IGrouping<string, ICoinContractFilter> filtersOfContract, string eventName)
		{
			var evnt = filtersOfContract?.FirstOrDefault(o => o.EventName == eventName);
			if (evnt != null) return evnt;
			return await CreateAndSaveFilter(settingsCoinContract, eventName);
		}

		private async Task<ICoinContractFilter> CreateAndSaveFilter(KeyValuePair<string, EthereumContract> settingsCoinContract, string eventName)
		{
			var filter = await _contractService.CreateFilter(settingsCoinContract.Key, settingsCoinContract.Value.Abi, eventName);
			var coinContractFilter = new CoinContractFilter
			{
				EventName = eventName,
				ContractAddress = settingsCoinContract.Key,
				Filter = filter.HexValue
			};
			await _coinContractFilterRepository.AddFilterAsync(coinContractFilter);
			return coinContractFilter;
		}

		public async Task RetrieveEventLogs(bool recreateFilters)
		{
			var filters = await GetCoinContractFilters(recreateFilters);
			foreach (var coinContractFilter in filters)
			{
				switch (coinContractFilter.EventName)
				{
					case Constants.CashInEvent:
						var cashInLogs = await _contractService.GetEvents<CoinContractCashInEvent>(coinContractFilter.ContractAddress,
							_settings.CoinContracts[coinContractFilter.ContractAddress].Abi, coinContractFilter.EventName,
							new HexBigInteger(coinContractFilter.Filter));
						cashInLogs?.ForEach(async o => await FireCoinContractEvent(coinContractFilter.ContractAddress, coinContractFilter.EventName, o.Caller, null, null, o.Amount));
						break;
					case Constants.CashOutEvent:
						(await _contractService.GetEvents<CoinContractCashOutEvent>(coinContractFilter.ContractAddress,
							_settings.CoinContracts[coinContractFilter.ContractAddress].Abi, coinContractFilter.EventName, new HexBigInteger(coinContractFilter.Filter)))
						?.ForEach(async o => await FireCoinContractEvent(coinContractFilter.ContractAddress, coinContractFilter.EventName, o.Caller, o.From, o.To, o.Amount));
						break;
					case Constants.TransferEvent:
						(await _contractService.GetEvents<CoinContractTransferEvent>(coinContractFilter.ContractAddress,
							_settings.CoinContracts[coinContractFilter.ContractAddress].Abi, coinContractFilter.EventName, new HexBigInteger(coinContractFilter.Filter)))
						?.ForEach(async o => await FireCoinContractEvent(coinContractFilter.ContractAddress, coinContractFilter.EventName, o.Caller, o.From, o.To, o.Amount));
						break;
				}
			}
		}

		private async Task FireCoinContractEvent(string contract, string eventName, string caller, string from, string to, BigInteger amount)
		{
			decimal convertedAmount = _settings.CoinContracts[contract].GetExternalValue(amount);

			await _coinEventQueue.PutRawMessageAsync(JsonConvert.SerializeObject(new CoinContractPublicEvent
			{
				EventName = eventName,
				Address = contract,
				Amount = convertedAmount,
				Caller = caller,
				From = from,
				To = to
			}));

		}

	}
}

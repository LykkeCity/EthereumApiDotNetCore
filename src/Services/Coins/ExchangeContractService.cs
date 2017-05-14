using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
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
using AzureStorage.Queue;
using Nethereum.Contracts;
using Nethereum.Util;
using SigningServiceApiCaller;

namespace Services.Coins
{
    public interface IExchangeContractService
    {
        Task<string> Swap(Guid id, string clientA, string clientB, string coinA, string coinB, decimal amountA, decimal amountB,
            string signAHex, string signBHex);

        Task<string> CashIn(Guid id, string coin, string receiver, BigInteger amount);

        Task<string> CashOut(Guid id, string coin, string clientAddr, string toAddr, BigInteger amount, string sign);

        Task<string> Transfer(Guid id, string coin, string from, string to, BigInteger amount, string sign);

        Task<string> CashinOverTransferContract(Guid id, string coin, string receiver, decimal amount);

        Task PingMainExchangeContract();

        Task<IEnumerable<ICoinContractFilter>> GetCoinContractFilters(bool recreate);

        Task RetrieveEventLogs(bool recreateFilters);

        Task<string> GetSign(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount);

        //Test sha3 and sign check on blockchain
        //Task<byte[]> CalculateHash(Guid guid, string adapterAddress, string clientAddress1, string clientAddress2, BigInteger currentBalance);
        //Task<bool> CheckSign(string clientAddress, byte[] hash, byte[] sign);
    }

    public class ExchangeContractService : IExchangeContractService
    {
        private readonly IBaseSettings _settings;
        private readonly ICoinTransactionService _cointTransactionService;
        private readonly IContractService _contractService;
        private readonly ICoinContractFilterRepository _coinContractFilterRepository;
        private readonly ICoinRepository _coinRepository;
        private readonly IQueueExt _coinEventQueue;
        private Web3 _web3;
        private readonly ILykkeSigningAPI _lykkeSigningAPI;

        public ExchangeContractService(IBaseSettings settings,
            ICoinTransactionService cointTransactionService, IContractService contractService,
            ICoinContractFilterRepository coinContractFilterRepository, Func<string, IQueueExt> queueFactory,
            ICoinRepository coinRepository, IEthereumContractRepository ethereumContractRepository, Web3 web3,
            ILykkeSigningAPI lykkeSigningAPI,)
        {
            _lykkeSigningAPI = lykkeSigningAPI;
            _web3 = web3;
            _settings = settings;
            _cointTransactionService = cointTransactionService;
            _contractService = contractService;
            _coinContractFilterRepository = coinContractFilterRepository;
            _coinRepository = coinRepository;
            _coinEventQueue = queueFactory(Constants.CoinEventQueue);
        }

        public async Task<string> Swap(Guid id, string clientA, string clientB, string coinA, string coinB, decimal amountA, decimal amountB, string signAHex,
            string signBHex)
        {
            var web3 = new Web3(_settings.EthereumUrl);

            await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount, _settings.EthereumMainAccountPassword, new HexBigInteger(120));

            var contract = web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);

            var coinAFromDb = await _coinRepository.GetCoin(coinA);
            var coinBFromDb = await _coinRepository.GetCoin(coinB);

            var convertedAmountA = amountA.ToBlockchainAmount(coinAFromDb.Multiplier);
            var convertedAmountB = amountB.ToBlockchainAmount(coinBFromDb.Multiplier);

            var convertedId = EthUtils.GuidToBigInteger(id);

            var swap = contract.GetFunction("swap");
            var tr = await swap.SendTransactionAsync(_settings.EthereumMainAccount, new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0),
                    convertedId, clientA, clientB, coinAFromDb.AdapterAddress, coinBFromDb.AdapterAddress, convertedAmountA, convertedAmountB, signAHex.HexToByteArray().FixByteOrder(), signBHex.HexToByteArray().FixByteOrder(), new byte[0]);
            await _cointTransactionService.PutTransactionToQueue(tr);
            return tr;
        }

        public async Task<string> CashIn(Guid id, string coinAddress, string receiver, BigInteger amount)
        {
            var web3 = new Web3(_settings.EthereumUrl);

            await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount, _settings.EthereumMainAccountPassword, 120);

            var coinAFromDb = await _coinRepository.GetCoinByAddress(coinAddress);

            if (coinAFromDb == null)
            {
                throw new Exception($"Coin with address {coinAddress} deos not exist");
            }

            Contract contract;
            if (coinAFromDb.ContainsEth)
            {
                contract = web3.Eth.GetContract(_settings.EthAdapterContract.Abi, coinAFromDb.AdapterAddress);
            }
            else
            {
                contract = web3.Eth.GetContract(_settings.TokenAdapterContract.Abi, coinAFromDb.AdapterAddress);
            }

            var convertedAmountA = amount;

            var convertedId = EthUtils.GuidToBigInteger(id);

            var cashin = contract.GetFunction("cashin");
            var res = await cashin.CallAsync<bool>(_settings.EthereumMainAccount, new HexBigInteger(Constants.GasForCoinTransaction),
                            new HexBigInteger(0), receiver, convertedAmountA);
            string tr;
            if (coinAFromDb.ContainsEth)
            {
                tr = await cashin.SendTransactionAsync(_settings.EthereumMainAccount, new HexBigInteger(Constants.GasForCoinTransaction),
                            new HexBigInteger(convertedAmountA), receiver, convertedAmountA);
            }
            else
            {
                tr = await cashin.SendTransactionAsync(_settings.EthereumMainAccount, new HexBigInteger(Constants.GasForCoinTransaction),
                            new HexBigInteger(0), receiver, convertedAmountA);
            }
            await _cointTransactionService.PutTransactionToQueue(tr);
            return tr;
        }

        public async Task<string> CashOut(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount, string sign)
        {
            var coinAFromDb = await _coinRepository.GetCoinByAddress(coinAddress);
            var convertedAmount = amount;
            if (string.IsNullOrEmpty(sign))
            {
                sign = await GetSign(id, coinAddress, clientAddr, toAddr, amount);
            }
            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
            var cashout = contract.GetFunction("cashout");

            var convertedId = EthUtils.GuidToBigInteger(id);
            // function cashout(uint id, address coinAddress, address client, address to, uint amount, bytes client_sign, bytes params) onlyowner {
            var tr = await cashout.SendTransactionAsync(_settings.EthereumMainAccount,
                        new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0),
                        convertedId, coinAFromDb.AdapterAddress, clientAddr, toAddr, convertedAmount, sign.HexToByteArray().FixByteOrder(), new byte[0]);
            await _cointTransactionService.PutTransactionToQueue(tr);
            return tr;

        }

        public async Task<string> Transfer(Guid id, string coinAddress, string from, string to, BigInteger amount, string sign)
        {
            var coinAFromDb = await _coinRepository.GetCoinByAddress(coinAddress);
            if (string.IsNullOrEmpty(sign))
            {
                sign = await GetSign(id, coinAddress, from, to, amount);
            }

            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
            var cashout = contract.GetFunction("transfer");

            var convertedId = EthUtils.GuidToBigInteger(id);
            var tr = await cashout.SendTransactionAsync(_settings.EthereumMainAccount,
                    new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0),
                    convertedId, coinAFromDb.AdapterAddress, from, to, amount, sign.HexToByteArray().FixByteOrder(), new byte[0]);
            await _cointTransactionService.PutTransactionToQueue(tr);
            return tr;
        }

        public async Task<string> CashinOverTransferContract(Guid id, string coin, string receiver, decimal amount)
        {
            var coinDb = await _coinRepository.GetCoin(coin);
            if (!coinDb.BlockchainDepositEnabled)
                throw new Exception("Coin must be payable");
            var contract = _web3.Eth.GetContract(_settings.TokenTransferContract.Abi, _settings.TokenTransferContract.Address);
            var cashin = contract.GetFunction("cashin");

            var blockchainAmount = amount.ToBlockchainAmount(coinDb.Multiplier);
            var convertedId = EthUtils.GuidToBigInteger(id);
            var tr = await cashin.SendTransactionAsync(_settings.EthereumMainAccount, new HexBigInteger(Constants.GasForCoinTransaction),
                        new HexBigInteger(0), convertedId, coinDb.AdapterAddress, receiver, blockchainAmount, Constants.GasForCoinTransaction, new byte[0]);
            return tr;
        }

        public async Task PingMainExchangeContract()
        {
            if (_settings.MainExchangeContract == null)
                return;

            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
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
                var filtersOfContract = savedFilters.ContainsKey(settingsCoinContract.Value.Address) ? savedFilters[settingsCoinContract.Value.Address] : null;
                result.Add(await GetOrCreateEvent(settingsCoinContract, filtersOfContract, Constants.CashInEvent));
                result.Add(await GetOrCreateEvent(settingsCoinContract, filtersOfContract, Constants.CashOutEvent));
                result.Add(await GetOrCreateEvent(settingsCoinContract, filtersOfContract, Constants.TransferEvent));
            }
            return result;
        }

        private async Task<ICoinContractFilter> GetOrCreateEvent(KeyValuePair<string, Core.Settings.EthereumContract> settingsCoinContract, IGrouping<string, ICoinContractFilter> filtersOfContract, string eventName)
        {
            var evnt = filtersOfContract?.FirstOrDefault(o => o.EventName == eventName);
            if (evnt != null) return evnt;
            return await CreateAndSaveFilter(settingsCoinContract, eventName);
        }

        private async Task<ICoinContractFilter> CreateAndSaveFilter(KeyValuePair<string, Core.Settings.EthereumContract> settingsCoinContract, string eventName)
        {
            var filter = await _contractService.CreateFilter(settingsCoinContract.Value.Address, settingsCoinContract.Value.Abi, eventName);
            var coinContractFilter = new CoinContractFilter
            {
                EventName = eventName,
                ContractAddress = settingsCoinContract.Value.Address,
                Filter = filter.HexValue,
                CoinName = settingsCoinContract.Key
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
                            _settings.CoinContracts[coinContractFilter.CoinName].Abi, coinContractFilter.EventName,
                            new HexBigInteger(coinContractFilter.Filter));
                        cashInLogs?.ForEach(async o => await FireCoinContractEvent(coinContractFilter.ContractAddress, coinContractFilter.EventName, o.Caller, null, null, o.Amount));
                        break;
                    case Constants.CashOutEvent:
                        (await _contractService.GetEvents<CoinContractCashOutEvent>(coinContractFilter.ContractAddress,
                            _settings.CoinContracts[coinContractFilter.CoinName].Abi, coinContractFilter.EventName, new HexBigInteger(coinContractFilter.Filter)))
                        ?.ForEach(async o => await FireCoinContractEvent(coinContractFilter.ContractAddress, coinContractFilter.EventName, o.Caller, o.From, o.To, o.Amount));
                        break;
                    case Constants.TransferEvent:
                        (await _contractService.GetEvents<CoinContractTransferEvent>(coinContractFilter.ContractAddress,
                            _settings.CoinContracts[coinContractFilter.CoinName].Abi, coinContractFilter.EventName, new HexBigInteger(coinContractFilter.Filter)))
                        ?.ForEach(async o => await FireCoinContractEvent(coinContractFilter.ContractAddress, coinContractFilter.EventName, o.Caller, o.From, o.To, o.Amount));
                        break;
                }
            }
        }

        private async Task FireCoinContractEvent(string coinAddress, string eventName, string caller, string from, string to, BigInteger amount)
        {
            var coin = await _coinRepository.GetCoinByAddress(coinAddress);
            decimal convertedAmount = amount.FromBlockchainAmount(coin.Multiplier);

            await _coinEventQueue.PutRawMessageAsync(JsonConvert.SerializeObject(new CoinContractPublicEvent
            {
                CoinName = coin.Id,
                EventName = eventName,
                Address = coin.AdapterAddress,
                Amount = convertedAmount,
                Caller = caller,
                From = from,
                To = to
            }));
        }

        public async Task<string> GetSign(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount)
        {
            var strForHash = EthUtils.GuidToByteArray(id).ToHex() +
                            coinAddress.HexToByteArray().ToHex() +
                            clientAddr.HexToByteArray().ToHex() +
                            toAddr.HexToByteArray().ToHex() +
                            EthUtils.BigIntToArrayWithPadding(amount).ToHex();

            var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());
            //var sign = Sign(hash, clientPrivateKey).ToHex();
            var response = await _lykkeSigningAPI.ApiEthereumSignHashPostAsync(new SigningServiceApiCaller.Models.EthereumHashSignRequest()
            {
                FromProperty = clientAddr,
                Hash = hash.ToHex()
            });

            if (response == null || string.IsNullOrEmpty(response.SignedHash))
            {
                throw new Exception("Current from addrss is unknown for sign service and sign was not provided");
            }

            return response.SignedHash;
        }


        //public async Task<byte[]> CalculateHash(Guid guid, string adapterAddress, string clientAddress1, string clientAddress2, BigInteger currentBalance)
        //{
        //    var web3 = new Web3(_settings.EthereumUrl);

        //    string abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"client_addr\",\"type\":\"address\"},{\"name\":\"hash\",\"type\":\"bytes32\"},{\"name\":\"sig\",\"type\":\"bytes\"}],\"name\":\"checkClientSign\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"coinContract\",\"type\":\"address\"},{\"name\":\"newMainContract\",\"type\":\"address\"}],\"name\":\"changeMainContractInCoin\",\"outputs\":[],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"id\",\"type\":\"uint256\"},{\"name\":\"coinAddress\",\"type\":\"address\"},{\"name\":\"client\",\"type\":\"address\"},{\"name\":\"to\",\"type\":\"address\"},{\"name\":\"amount\",\"type\":\"uint256\"}],\"name\":\"getHash\",\"outputs\":[{\"name\":\"\",\"type\":\"bytes32\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"id\",\"type\":\"uint256\"},{\"name\":\"coinAddress\",\"type\":\"address\"},{\"name\":\"from\",\"type\":\"address\"},{\"name\":\"to\",\"type\":\"address\"},{\"name\":\"amount\",\"type\":\"uint256\"},{\"name\":\"sign\",\"type\":\"bytes\"},{\"name\":\"params\",\"type\":\"bytes\"}],\"name\":\"transfer\",\"outputs\":[],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[],\"name\":\"ping\",\"outputs\":[],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"name\":\"transactions\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"id\",\"type\":\"uint256\"},{\"name\":\"client_a\",\"type\":\"address\"},{\"name\":\"client_b\",\"type\":\"address\"},{\"name\":\"coinAddress_a\",\"type\":\"address\"},{\"name\":\"coinAddress_b\",\"type\":\"address\"},{\"name\":\"amount_a\",\"type\":\"uint256\"},{\"name\":\"amount_b\",\"type\":\"uint256\"},{\"name\":\"client_a_sign\",\"type\":\"bytes\"},{\"name\":\"client_b_sign\",\"type\":\"bytes\"},{\"name\":\"params\",\"type\":\"bytes\"}],\"name\":\"swap\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"id\",\"type\":\"uint256\"},{\"name\":\"coinAddress\",\"type\":\"address\"},{\"name\":\"client\",\"type\":\"address\"},{\"name\":\"to\",\"type\":\"address\"},{\"name\":\"amount\",\"type\":\"uint256\"},{\"name\":\"client_sign\",\"type\":\"bytes\"},{\"name\":\"params\",\"type\":\"bytes\"}],\"name\":\"cashout\",\"outputs\":[],\"payable\":false,\"type\":\"function\"},{\"inputs\":[],\"payable\":false,\"type\":\"constructor\"}]";
        //    var contract = web3.Eth.GetContract(abi, "0x7ede1e07cc39ef400472c1af7d1f58c064bc23dc");

        //    var getHash = contract.GetFunction("getHash");
        //    byte[] hash = await getHash.CallAsync<byte[]>(EthUtils.GuidToBigInteger(guid), adapterAddress, clientAddress1, clientAddress2, currentBalance);
        //    return hash;
        //}

        //public async Task<bool> CheckSign(string clientAddress, byte[] hash, byte[] sign)
        //{
        //    string abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"client_addr\",\"type\":\"address\"},{\"name\":\"hash\",\"type\":\"bytes32\"},{\"name\":\"sig\",\"type\":\"bytes\"}],\"name\":\"checkClientSign\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"coinContract\",\"type\":\"address\"},{\"name\":\"newMainContract\",\"type\":\"address\"}],\"name\":\"changeMainContractInCoin\",\"outputs\":[],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"id\",\"type\":\"uint256\"},{\"name\":\"coinAddress\",\"type\":\"address\"},{\"name\":\"client\",\"type\":\"address\"},{\"name\":\"to\",\"type\":\"address\"},{\"name\":\"amount\",\"type\":\"uint256\"}],\"name\":\"getHash\",\"outputs\":[{\"name\":\"\",\"type\":\"bytes32\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"id\",\"type\":\"uint256\"},{\"name\":\"coinAddress\",\"type\":\"address\"},{\"name\":\"from\",\"type\":\"address\"},{\"name\":\"to\",\"type\":\"address\"},{\"name\":\"amount\",\"type\":\"uint256\"},{\"name\":\"sign\",\"type\":\"bytes\"},{\"name\":\"params\",\"type\":\"bytes\"}],\"name\":\"transfer\",\"outputs\":[],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[],\"name\":\"ping\",\"outputs\":[],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"name\":\"transactions\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"id\",\"type\":\"uint256\"},{\"name\":\"client_a\",\"type\":\"address\"},{\"name\":\"client_b\",\"type\":\"address\"},{\"name\":\"coinAddress_a\",\"type\":\"address\"},{\"name\":\"coinAddress_b\",\"type\":\"address\"},{\"name\":\"amount_a\",\"type\":\"uint256\"},{\"name\":\"amount_b\",\"type\":\"uint256\"},{\"name\":\"client_a_sign\",\"type\":\"bytes\"},{\"name\":\"client_b_sign\",\"type\":\"bytes\"},{\"name\":\"params\",\"type\":\"bytes\"}],\"name\":\"swap\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"id\",\"type\":\"uint256\"},{\"name\":\"coinAddress\",\"type\":\"address\"},{\"name\":\"client\",\"type\":\"address\"},{\"name\":\"to\",\"type\":\"address\"},{\"name\":\"amount\",\"type\":\"uint256\"},{\"name\":\"client_sign\",\"type\":\"bytes\"},{\"name\":\"params\",\"type\":\"bytes\"}],\"name\":\"cashout\",\"outputs\":[],\"payable\":false,\"type\":\"function\"},{\"inputs\":[],\"payable\":false,\"type\":\"constructor\"}]";
        //    var contract = _web3.Eth.GetContract(abi, "0x7ede1e07cc39ef400472c1af7d1f58c064bc23dc");

        //    var checkClientSign = contract.GetFunction("checkClientSign");
        //    bool result = await checkClientSign.CallAsync<bool>(clientAddress, hash, sign);
        //    return result;
        //}
    }
}

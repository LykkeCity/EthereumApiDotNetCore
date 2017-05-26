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
using Core.Common;
using SigningServiceApiCaller.Models;
using Core.Exceptions;
using Nethereum.Signer;

namespace Services.Coins
{
    public interface IExchangeContractService
    {
        Task<string> Swap(Guid id, string clientA, string clientB, string coinA, string coinB, decimal amountA, decimal amountB,
            string signAHex, string signBHex);

        Task<string> CashIn(Guid id, string coin, string receiver, BigInteger amount);

        Task<string> CashOut(Guid id, string coin, string clientAddr, string toAddr, BigInteger amount, string sign);

        Task<string> Transfer(Guid id, string coin, string from, string to, BigInteger amount, string sign);

        Task<string> TransferWithChange(Guid id, string coinAddress, string from, string to, BigInteger amount,
            string signFrom, BigInteger change, string signTo);

        Task<string> CashinOverTransferContract(Guid id, string coin, string receiver, decimal amount);

        Task PingMainExchangeContract();

        Task<IEnumerable<ICoinContractFilter>> GetCoinContractFilters(bool recreate);

        Task RetrieveEventLogs(bool recreateFilters);

        Task<string> GetSign(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount);

        Task<IdCheckResult> CheckId(Guid guidToCheck);

        Task<bool> CheckSign(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount, string sign);
    }

    public class ExchangeContractService : IExchangeContractService
    {
        private readonly IBaseSettings _settings;
        private readonly ICoinTransactionService _cointTransactionService;
        private readonly IContractService _contractService;
        private readonly ICoinContractFilterRepository _coinContractFilterRepository;
        private readonly ICoinRepository _coinRepository;
        private readonly IQueueExt _coinEventQueue;
        private readonly Web3 _web3;
        private readonly ILykkeSigningAPI _lykkeSigningAPI;
        private readonly IUserPaymentHistoryRepository _userPaymentHistoryRepository;
        private readonly ICoinEventService _coinEventService;
        private readonly IHashCalculator _hashCalculator;

        public ExchangeContractService(IBaseSettings settings,
            ICoinTransactionService cointTransactionService, IContractService contractService,
            ICoinContractFilterRepository coinContractFilterRepository, Func<string, IQueueExt> queueFactory,
            ICoinRepository coinRepository, IEthereumContractRepository ethereumContractRepository, Web3 web3,
            ILykkeSigningAPI lykkeSigningAPI, 
            IUserPaymentHistoryRepository userPaymentHistory, 
            ICoinEventService coinEventService, 
            IHashCalculator hashCalculator)
        {
            _lykkeSigningAPI = lykkeSigningAPI;
            _web3 = web3;
            _settings = settings;
            _cointTransactionService = cointTransactionService;
            _contractService = contractService;
            _coinContractFilterRepository = coinContractFilterRepository;
            _coinRepository = coinRepository;
            _coinEventQueue = queueFactory(Constants.CoinEventQueue);
            _userPaymentHistoryRepository = userPaymentHistory;
            _coinEventService = coinEventService;
            _hashCalculator = hashCalculator;
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
            await ThrowOnExistingId(id);
            var coinAFromDb = await GetCoinWithCheck(coinAddress);

            if (string.IsNullOrEmpty(sign))
            {
                sign = await GetSign(id, coinAddress, clientAddr, toAddr, amount);
            }

            await ThrowOnWrongSignature(id, coinAddress, clientAddr, toAddr, amount, sign);

            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
            var cashout = contract.GetFunction("cashout");

            var convertedId = EthUtils.GuidToBigInteger(id);
            // function cashout(uint id, address coinAddress, address client, address to, uint amount, bytes client_sign, bytes params) onlyowner {
            var transactionHash = await cashout.SendTransactionAsync(_settings.EthereumMainAccount,
                        new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0),
                        convertedId, coinAFromDb.AdapterAddress, clientAddr, toAddr, amount, sign.HexToByteArray().FixByteOrder(), new byte[0]);
            await SaveUserHistory(coinAddress, amount.ToString(), clientAddr, toAddr, transactionHash, "CashOut");
            await _coinEventService.PublishEvent(new CoinEvent(transactionHash, clientAddr, toAddr,
                amount.ToString(), CoinEventType.CashoutStarted, coinAddress));

            return transactionHash;

        }

        public async Task<string> Transfer(Guid id, string coinAddress, string from, string to, BigInteger amount, string sign)
        {
            await ThrowOnExistingId(id);
            var coinAFromDb = await GetCoinWithCheck(coinAddress);

            if (string.IsNullOrEmpty(sign))
            {
                sign = await GetSign(id, coinAddress, from, to, amount);
            }

            await ThrowOnWrongSignature(id, coinAddress, from, to, amount, sign);

            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
            var transferFunction = contract.GetFunction("transfer");

            var convertedId = EthUtils.GuidToBigInteger(id);
            var transactionHash = await transferFunction.SendTransactionAsync(_settings.EthereumMainAccount,
                    new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0),
                    convertedId, coinAFromDb.AdapterAddress, from, to, amount, sign.HexToByteArray().FixByteOrder(), new byte[0]);
            await SaveUserHistory(coinAddress, amount.ToString(), from, to, transactionHash, "Transfer");
            await _coinEventService.PublishEvent(new CoinEvent(transactionHash, from, to,
                amount.ToString(), CoinEventType.TransferStarted, coinAddress));

            return transactionHash;
        }

        public async Task<string> TransferWithChange(Guid id, string coinAddress, string from, string to, BigInteger amount,
            string signFrom, BigInteger change, string signTo)
        {
            if (amount <= change)
            {
                throw new ClientSideException(ExceptionType.WrongParams, "Amount can't be less or equal than change");
            }

            await ThrowOnExistingId(id);
            var coinAFromDb = await GetCoinWithCheck(coinAddress);

            if (string.IsNullOrEmpty(signFrom))
            {
                signFrom = await GetSign(id, coinAddress, from, to, amount);
            }

            if (string.IsNullOrEmpty(signTo))
            {
                signTo = await GetSign(id, coinAddress, to, from, change);
            }

            await ThrowOnWrongSignature(id, coinAddress, from, to, amount, signFrom);
            await ThrowOnWrongSignature(id, coinAddress, to, from, change, signTo);

            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
            var transferFunction = contract.GetFunction("transferWithChange");
            var convertedId = EthUtils.GuidToBigInteger(id);
            var transactionHash = await transferFunction.SendTransactionAsync(_settings.EthereumMainAccount,
                    new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0),
                    convertedId, coinAFromDb.AdapterAddress, from, to, amount, change,
                    signFrom.HexToByteArray().FixByteOrder(), signTo.HexToByteArray().FixByteOrder(), new byte[0]);
            var difference = (amount - change);

            await SaveUserHistory(coinAddress, difference.ToString(), from, to, transactionHash, "TransferWithChange");
            await _coinEventService.PublishEvent(new CoinEvent(transactionHash, from, to,
                difference.ToString(), CoinEventType.TransferStarted, coinAddress));

            return transactionHash;
        }

        public async Task<string> CashinOverTransferContract(Guid id, string coin, string receiver, decimal amount)
        {
            var coinDb = await _coinRepository.GetCoin(coin);
            if (!coinDb.BlockchainDepositEnabled)
            {
                throw new ClientSideException(ExceptionType.WrongParams, "Coin must be payable");
            }
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
            string transactionHash = await ping.SendTransactionAsync(_settings.EthereumMainAccount);
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

        public async Task<IdCheckResult> CheckId(Guid guidToCheck)
        {
            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);

            var transactionsCheck = contract.GetFunction("transactions");
            bool isInList = true;
            Guid useNext = guidToCheck;
            while (isInList)
            {
                var bigIntRepresentation = EthUtils.GuidToBigInteger(useNext);
                isInList = await transactionsCheck.CallAsync<bool>(bigIntRepresentation);
                if (isInList)
                {
                    useNext = Guid.NewGuid();
                }
            }

            return new IdCheckResult()
            {
                IsFree = useNext == guidToCheck,
                ProposedId = useNext
            };
        }

        public async Task<bool> CheckSign(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount, string sign)
        {
            if (string.IsNullOrEmpty(sign))
            {
                return false;
            }

            var fixedSign = sign.EnsureHexPrefix();
            var hash = GetHash(id, coinAddress, clientAddr, toAddr, amount);
            var signer = new MessageSigner();
            string sender = signer.EcRecover(hash, sign);

            return clientAddr == sender;
        }

        public async Task<string> GetSign(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount)
        {
            byte[] hash = GetHash(id, coinAddress, clientAddr, toAddr, amount);
            HashSignResponse response;
            try
            {
                response = await _lykkeSigningAPI.ApiEthereumSignHashPostAsync(new SigningServiceApiCaller.Models.EthereumHashSignRequest()
                {
                    FromProperty = clientAddr,
                    Hash = hash.ToHex()
                });

                if (response == null || string.IsNullOrEmpty(response.SignedHash))
                {
                    throw new Exception();
                }
            }
            catch (Exception e)
            {
                throw new ClientSideException(ExceptionType.WrongSign, "Current from address is unknown for sign service and sign was not provided");
            }

            return response.SignedHash;
        }

        private byte[] GetHash(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount)
        {
            return _hashCalculator.GetHash(id, coinAddress, clientAddr, toAddr, amount);
        }

        private async Task SaveUserHistory(string adapterAddress, string amount, string userAddress, string toAddress, string trHash, string note)
        {
            await _userPaymentHistoryRepository.SaveAsync(new UserPaymentHistory()
            {
                AdapterAddress = adapterAddress,
                Amount = amount,
                ToAddress = toAddress,
                CreatedDate = DateTime.UtcNow,
                Note = note,
                TransactionHash = trHash,
                UserAddress = userAddress
            });
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

        private static EthECDSASignature ExtractEcdsaSignature(string signature)
        {
            var signatureArray = signature.HexToByteArray();

            var v = signatureArray[64];

            if ((v == 0) || (v == 1))
                v = (byte)(v + 27);

            var r = new byte[32];
            Array.Copy(signatureArray, r, 32);
            var s = new byte[32];
            Array.Copy(signatureArray, 32, s, 0, 32);

            var ecdaSignature = EthECDSASignatureFactory.FromComponents(r, s, v);
            return ecdaSignature;
        }

        private async Task ThrowOnWrongSignature(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount, string sign)
        {
            var checkSign = await CheckSign(id, coinAddress, clientAddr, toAddr, amount, sign);
            if (!checkSign)
            {
                throw new ClientSideException(ExceptionType.WrongSign, "");
            }
        }

        private async Task<ICoin> GetCoinWithCheck(string coinAddress)
        {
            var coin = await _coinRepository.GetCoinByAddress(coinAddress);

            if (coinAddress == null)
            {
                throw new ClientSideException(ExceptionType.WrongParams, $"Coin with address {coinAddress}");
            }

            return coin;
        }

        private async Task ThrowOnExistingId(Guid id)
        {
            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
            var transactionsCheck = contract.GetFunction("transactions");
            var bigIntRepresentation = EthUtils.GuidToBigInteger(id);

            bool isInList = await transactionsCheck.CallAsync<bool>(bigIntRepresentation);

            if (isInList)
            {
                throw new ClientSideException(ExceptionType.OperationWithIdAlreadyExists, $"operation with guid {id}");
            }
        }
    }
}

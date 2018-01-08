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
using Services.Model;
using System.Text.RegularExpressions;

namespace Services.Coins
{
    public interface IExchangeContractService
    {
        //Task<string> Swap(Guid id, string clientA, string clientB, string coinA, string coinB, decimal amountA, decimal amountB,
        //    string signAHex, string signBHex);
        bool IsValidAddress(string address);

        Task<string> CashIn(Guid id, string coin, string receiver, BigInteger amount);

        Task<string> CashOut(Guid id, string coin, string clientAddr, string toAddr, BigInteger amount, string sign);

        Task<string> Transfer(Guid id, string coin, string from, string to, BigInteger amount, string sign);

        Task<string> TransferWithChange(Guid id, string coinAddress, string from, string to, BigInteger amount,
            string signFrom, BigInteger change, string signTo);

        Task<string> TransferWithoutSignCheck(Guid id, string coinAddress, string from, string to, BigInteger amount, string sign);

        Task<string> CashinOverTransferContract(Guid id, string coin, string receiver, decimal amount);

        Task<string> PingMainExchangeContract();

        Task<string> GetSign(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount);

        Task<IdCheckResult> CheckId(Guid guidToCheck);

        bool CheckSign(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount, string sign);
        Task<bool> CheckLastTransactionCompleted(string coinAddress, string clientAddr);
        Task<OperationEstimationResult> EstimateCashoutGas(Guid id, string coinAdapterAddress, string fromAddress, string toAddress, BigInteger amount, string sign);
        Task<string> ChangeMainContractInCoin(string coinAddress, string newExchangeContractAddress, string newMainExchangeAbi);
    }

    public class ExchangeContractService : IExchangeContractService
    {
        private readonly IBaseSettings _settings;
        private readonly ICoinTransactionService _cointTransactionService;
        private readonly IContractService _contractService;
        private readonly ICoinContractFilterRepository _coinContractFilterRepository;
        private readonly ICoinRepository _coinRepository;
        private readonly Web3 _web3;
        private readonly ILykkeSigningAPI _lykkeSigningAPI;
        private readonly IUserPaymentHistoryRepository _userPaymentHistoryRepository;
        private readonly ICoinEventService _coinEventService;
        private readonly IHashCalculator _hashCalculator;
        private readonly IPendingTransactionsRepository _pendingTransactionsRepository;
        private readonly ITransferContractService _transferContractService;
        private readonly AddressUtil _addressUtil;

        public ExchangeContractService(IBaseSettings settings,
            ICoinTransactionService cointTransactionService, IContractService contractService,
            ICoinContractFilterRepository coinContractFilterRepository, Func<string, IQueueExt> queueFactory,
            ICoinRepository coinRepository, IEthereumContractRepository ethereumContractRepository, Web3 web3,
            ILykkeSigningAPI lykkeSigningAPI,
            IUserPaymentHistoryRepository userPaymentHistory,
            ICoinEventService coinEventService,
            IHashCalculator hashCalculator,
            IPendingTransactionsRepository pendingTransactionsRepository,
            ITransferContractService transferContractService)
        {
            _lykkeSigningAPI = lykkeSigningAPI;
            _web3 = web3;
            _settings = settings;
            _cointTransactionService = cointTransactionService;
            _contractService = contractService;
            _coinContractFilterRepository = coinContractFilterRepository;
            _coinRepository = coinRepository;
            _userPaymentHistoryRepository = userPaymentHistory;
            _coinEventService = coinEventService;
            _hashCalculator = hashCalculator;
            _pendingTransactionsRepository = pendingTransactionsRepository;
            _transferContractService = transferContractService;
            _addressUtil = new AddressUtil();
        }

        public bool IsValidAddress(string address)
        {
            if (!new Regex("^(0x)?[0-9a-f]{40}$", RegexOptions.IgnoreCase).IsMatch(address))
            {
                // check if it has the basic requirements of an address
                return false;
            }
            else if (new Regex("^(0x)?[0-9a-f]{40}$").IsMatch(address) ||
                new Regex("^(0x)?[0-9A-F]{40}$").IsMatch(address))
            {
                // If it's all small caps or all all caps, return true
                return true;
            }
            else
            {
                // Check each case
                return _addressUtil.IsChecksumAddress(address);
            };
        }

        public async Task<OperationEstimationResult> EstimateCashoutGas(Guid id, string coinAdapterAddress, string fromAddress, string toAddress, BigInteger amount, string sign)
        {
            var coinAFromDb = await GetCoinWithCheck(coinAdapterAddress);

            if (string.IsNullOrEmpty(sign))
            {
                sign = await GetSign(id, coinAdapterAddress, fromAddress, toAddress, amount);
            }

            ThrowOnWrongSignature(id, coinAdapterAddress, fromAddress, toAddress, amount, sign);

            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
            var cashout = contract.GetFunction("cashout");
            var convertedId = EthUtils.GuidToBigInteger(id);
            //ACTION
            var estimatedGasForOperation = await cashout.EstimateGasAsync(_settings.EthereumMainAccount,
                        new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0), 
                        convertedId,
                        _addressUtil.ConvertToChecksumAddress(coinAFromDb.AdapterAddress), 
                        fromAddress, 
                        toAddress,
                        amount, 
                        sign.HexToByteArray(), 
                        new byte[0]);

            return new OperationEstimationResult()
            {
                GasAmount = estimatedGasForOperation.Value,
                IsAllowed = estimatedGasForOperation.Value < Constants.GasForCoinTransaction
            };
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

            ThrowOnWrongSignature(id, coinAddress, clientAddr, toAddr, amount, sign);

            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
            var cashout = contract.GetFunction("cashout");

            var convertedId = EthUtils.GuidToBigInteger(id);
            // function cashout(uint id, address coinAddress, address client, address to, uint amount, bytes client_sign, bytes params) onlyowner {
            var transactionHash = await cashout.SendTransactionAsync(Constants.AddressForRoundRobinTransactionSending,
                        new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0),
                        convertedId, coinAFromDb.AdapterAddress, clientAddr, toAddr, amount, sign.HexToByteArray(), new byte[0]);
            await SaveUserHistory(coinAddress, amount.ToString(), clientAddr, toAddr, transactionHash, "CashOut");
            await CreatePendingTransaction(coinAddress, clientAddr, transactionHash);

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

            ThrowOnWrongSignature(id, coinAddress, from, to, amount, sign);

            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
            var transferFunction = contract.GetFunction("transfer");

            var convertedId = EthUtils.GuidToBigInteger(id);
            var transactionHash = await transferFunction.SendTransactionAsync(Constants.AddressForRoundRobinTransactionSending,
                    new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0),
                    convertedId, coinAFromDb.AdapterAddress, from, to, amount, sign.HexToByteArray(), new byte[0]);
            await SaveUserHistory(coinAddress, amount.ToString(), from, to, transactionHash, "Transfer");
            await CreatePendingTransaction(coinAddress, from, transactionHash);

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

            ThrowOnWrongSignature(id, coinAddress, from, to, amount, signFrom);
            ThrowOnWrongSignature(id, coinAddress, to, from, change, signTo);

            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
            var transferFunction = contract.GetFunction("transferWithChange");
            var convertedId = EthUtils.GuidToBigInteger(id);
            var transactionHash = await transferFunction.SendTransactionAsync(Constants.AddressForRoundRobinTransactionSending,
                    new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0),
                    convertedId, coinAFromDb.AdapterAddress, from, to, amount, change,
                    signFrom.HexToByteArray(), signTo.HexToByteArray(), new byte[0]);
            var difference = (amount - change);

            await SaveUserHistory(coinAddress, difference.ToString(), from, to, transactionHash, "TransferWithChange");
            await CreatePendingTransaction(coinAddress, from, transactionHash);

            return transactionHash;
        }

        public async Task<string> TransferWithoutSignCheck(Guid id, string coinAddress, string from, string to, BigInteger amount, string sign)
        {
            await ThrowOnExistingId(id);
            var coinAFromDb = await GetCoinWithCheck(coinAddress);

            if (string.IsNullOrEmpty(sign))
            {
                sign = await GetSign(id, coinAddress, from, to, amount);
            }

            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
            var transferFunction = contract.GetFunction("transfer");

            var convertedId = EthUtils.GuidToBigInteger(id);
            var transactionHash = await transferFunction.SendTransactionAsync(_settings.EthereumMainAccount,
                    new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0),
                    convertedId, coinAFromDb.AdapterAddress, from, to, amount, sign.HexToByteArray(), new byte[0]);
            await SaveUserHistory(coinAddress, amount.ToString(), from, to, transactionHash, "Transfer");
            await CreatePendingTransaction(coinAddress, from, transactionHash);

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

        public async Task<string> PingMainExchangeContract()
        {
            if (_settings.MainExchangeContract == null)
                return null;

            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
            var ping = contract.GetFunction("ping");
            string transactionHash = await ping.SendTransactionAsync(_settings.EthereumMainAccount);

            return transactionHash;
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

        public bool CheckSign(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount, string sign)
        {
            if (string.IsNullOrEmpty(sign))
            {
                return false;
            }

            var fixedSign = sign.EnsureHexPrefix();
            var hash = GetHash(id, coinAddress, clientAddr, toAddr, amount);
            var signer = new MessageSigner();
            string sender = signer.EcRecover(hash, sign);
            var util = new AddressUtil();
            string checksumClientAddr = util.ConvertToChecksumAddress(clientAddr);
            string checksumSender = util.ConvertToChecksumAddress(sender);

            return checksumClientAddr == checksumSender;
        }

        public async Task<bool> CheckLastTransactionCompleted(string coinAddress, string clientAddr)
        {
            IPendingTransaction pendingTransaction = await _pendingTransactionsRepository.GetAsync(coinAddress, clientAddr);

            return pendingTransaction == null;
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

        public async Task<string> ChangeMainContractInCoin(string coinAddress, string newExchangeContractAddress, string newMainExchangeAbi)
        {
            var coinAFromDb = await GetCoinWithCheck(coinAddress);
            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
            var transferFunction = contract.GetFunction("changeMainContractInCoin");
            var transactionHash = await transferFunction.SendTransactionAsync(_settings.EthereumMainAccount,
                    new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0), coinAddress, newExchangeContractAddress);

            return transactionHash;
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

        private void ThrowOnWrongSignature(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount, string sign)
        {
            var checkSign = CheckSign(id, coinAddress, clientAddr, toAddr, amount, sign);
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

        private async Task CreatePendingTransaction(string coinAddress, string clientAddr, string transactionHash)
        {
            await _pendingTransactionsRepository.InsertOrReplace(new PendingTransaction() { CoinAdapterAddress = coinAddress, UserAddress = clientAddr, TransactionHash = transactionHash });
        }
    }
}

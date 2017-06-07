using Core;
using Core.Exceptions;
using Core.Repositories;
using Core.Settings;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Util;
using Core.Utils;
using SigningServiceApiCaller.Models;
using SigningServiceApiCaller;
using Services.New.Models;
using Newtonsoft.Json;

namespace Services
{
    public interface IPendingOperationService
    {
        Task<string> CashOut(Guid id, string coin, string clientAddr, string toAddr, BigInteger amount, string sign);

        Task<string> Transfer(Guid id, string coin, string from, string to, BigInteger amount, string sign);

        Task<string> TransferWithChange(Guid id, string coinAddress, string from, string to, BigInteger amount,
            string signFrom, BigInteger change, string signTo);

        Task<IPendingOperation> GetOperationAsync(string operationId);
        Task RefreshOperationAsync(string hash);
    }

    public class PendingOperationService : IPendingOperationService
    {
        private readonly IBaseSettings _settings;
        private readonly IOperationToHashMatchRepository _operationToHashMatchRepository;
        private readonly IPendingOperationRepository _pendingOperationRepository;
        private readonly IQueueExt _queue;
        private readonly AddressUtil _util;
        private readonly Web3 _web3;
        private readonly IHashCalculator _hashCalculator;
        private readonly ICoinRepository _coinRepository;
        private readonly ILykkeSigningAPI _lykkeSigningAPI;

        public PendingOperationService(IBaseSettings settings, IOperationToHashMatchRepository operationToHashMatchRepository,
            IPendingOperationRepository pendingOperationRepository, IQueueFactory queueFactory, Web3 web3, IHashCalculator hashCalculator,
            ICoinRepository coinRepository, ILykkeSigningAPI lykkeSigningAPI)
        {
            _web3 = web3;
            _settings = settings;
            _pendingOperationRepository = pendingOperationRepository;
            _operationToHashMatchRepository = operationToHashMatchRepository;
            _queue = queueFactory.Build(Constants.PendingOperationsQueue);
            _util = new AddressUtil();
            _hashCalculator = hashCalculator;
            _coinRepository = coinRepository;
            _lykkeSigningAPI = lykkeSigningAPI;
        }

        public async Task<string> CashOut(Guid id, string coinAddress, string fromAddress, string toAddress, BigInteger amount, string sign)
        {
            await ThrowOnExistingId(id);
            var coinAFromDb = await GetCoinWithCheck(coinAddress);

            if (string.IsNullOrEmpty(sign))
            {
                sign = await GetSign(id, coinAddress, fromAddress, toAddress, amount);
            }

            ThrowOnWrongSignature(id, coinAddress, fromAddress, toAddress, amount, sign);

            var opId = await CreateOperation(new PendingOperation()
            {
                OperationId = id.ToString(),
                Amount = amount.ToString(),
                CoinAdapterAddress = coinAddress,
                FromAddress = fromAddress,
                OperationType = OperationTypes.Cashout,
                SignFrom = sign,
                SignTo = null,
                ToAddress = toAddress,
                MainExchangeId = id,
            });

            return opId;
        }

        public async Task<string> Transfer(Guid id, string coinAddress, string fromAddress, string toAddress, BigInteger amount, string sign)
        {
            await ThrowOnExistingId(id);
            var coinAFromDb = await GetCoinWithCheck(coinAddress);

            if (string.IsNullOrEmpty(sign))
            {
                sign = await GetSign(id, coinAddress, fromAddress, toAddress, amount);
            }

            ThrowOnWrongSignature(id, coinAddress, fromAddress, toAddress, amount, sign);

            var opId = await CreateOperation(new PendingOperation()
            {
                OperationId = id.ToString(),
                Amount = amount.ToString(),
                CoinAdapterAddress = coinAddress,
                FromAddress = fromAddress,
                OperationType = OperationTypes.Transfer,
                SignFrom = sign,
                SignTo = null,
                ToAddress = toAddress,
                MainExchangeId = id,
            });

            return opId;
        }

        public async Task<string> TransferWithChange(Guid id, string coinAddress, string fromAddress, string toAddress,
            BigInteger amount, string signFrom, BigInteger change, string signTo)
        {
            if (amount <= change)
            {
                throw new ClientSideException(ExceptionType.WrongParams, "Amount can't be less or equal than change");
            }

            await ThrowOnExistingId(id);
            var coinAFromDb = await GetCoinWithCheck(coinAddress);

            if (string.IsNullOrEmpty(signFrom))
            {
                signFrom = await GetSign(id, coinAddress, fromAddress, toAddress, amount);
            }

            if (string.IsNullOrEmpty(signTo))
            {
                signTo = await GetSign(id, coinAddress, toAddress, fromAddress, change);
            }

            ThrowOnWrongSignature(id, coinAddress, fromAddress, toAddress, amount, signFrom);
            ThrowOnWrongSignature(id, coinAddress, toAddress, fromAddress, change, signTo);

            var opId = await CreateOperation(new PendingOperation()
            {
                OperationId = id.ToString(),
                Change = change.ToString(),
                Amount = amount.ToString(),
                CoinAdapterAddress = coinAddress,
                FromAddress = fromAddress,
                OperationType = OperationTypes.TransferWithChange,
                SignFrom = signFrom,
                SignTo = signTo,
                ToAddress = toAddress,
                MainExchangeId = id,
            });

            return opId;
        }

        public async Task<IPendingOperation> GetOperationAsync(string operationId)
        {
            IOperationToHashMatch match = await _operationToHashMatchRepository.GetAsync(operationId);
            if (match == null)
            {
                return null;
            }

            IPendingOperation operation = await _pendingOperationRepository.GetOperation(operationId);
            return operation;
        }

        public async Task RefreshOperationAsync(string trHash)
        {
            IOperationToHashMatch match = await _operationToHashMatchRepository.GetByHashAsync(trHash);
            if (match == null)
            {
                return;
            }

            IPendingOperation operation = await _pendingOperationRepository.GetOperation(match.OperationId);
            match.TransactionHash = "";

            await _operationToHashMatchRepository.InsertOrReplaceAsync(match);
            await _queue.PutMessageAsync(new OperationHashMatchMessage() { OperationId = match.OperationId });
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

        private async Task<ICoin> GetCoinWithCheck(string coinAddress)
        {
            var coin = await _coinRepository.GetCoinByAddress(coinAddress);

            if (coinAddress == null)
            {
                throw new ClientSideException(ExceptionType.WrongParams, $"Coin with address {coinAddress}");
            }

            return coin;
        }

        private void ThrowOnWrongSignature(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount, string sign)
        {
            var checkSign = CheckSign(id, coinAddress, clientAddr, toAddr, amount, sign);
            if (!checkSign)
            {
                throw new ClientSideException(ExceptionType.WrongSign, "");
            }
        }

        private bool CheckSign(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount, string sign)
        {
            if (string.IsNullOrEmpty(sign))
            {
                return false;
            }

            var fixedSign = sign.EnsureHexPrefix();
            var hash = GetHash(id, coinAddress, clientAddr, toAddr, amount);
            var signer = new MessageSigner();
            string sender = signer.EcRecover(hash, sign);
            string checksumClientAddr = _util.ConvertToChecksumAddress(clientAddr);
            string checksumSender = _util.ConvertToChecksumAddress(sender);

            return checksumClientAddr == checksumSender;
        }

        private byte[] GetHash(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount)
        {
            return _hashCalculator.GetHash(id, coinAddress, clientAddr, toAddr, amount);
        }

        private async Task<string> CreateOperation(IPendingOperation operation)
        {
            var op = new PendingOperation()
            {
                OperationId = operation.OperationId,
                Amount = operation.Amount,
                CoinAdapterAddress = operation.CoinAdapterAddress,
                FromAddress = operation.FromAddress,
                OperationType = operation.OperationType,
                SignFrom = operation.SignFrom,
                SignTo = operation.SignTo,
                ToAddress = operation.ToAddress,
            };

            var match = new OperationToHashMatch()
            {
                OperationId = op.OperationId,
                TransactionHash = ""
            };

            await _operationToHashMatchRepository.InsertOrReplaceAsync(match);

            await _pendingOperationRepository.InsertOrReplace(op);
            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject( new OperationHashMatchMessage() { OperationId = op.OperationId }));

            return op.OperationId;
        }

        private async Task<string> GetSign(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount)
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
    }
}

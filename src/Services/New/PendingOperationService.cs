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
using Common.Log;
using Core.Notifiers;

namespace Services
{
    public interface IPendingOperationService
    {
        Task<string> CashOut(Guid id, string coin, string clientAddr, string toAddr, BigInteger amount, string sign);

        Task<string> Transfer(Guid id, string coin, string from, string to, BigInteger amount, string sign);

        Task<string> TransferWithChange(Guid id, string coinAddress, string from, string to, BigInteger amount,
            string signFrom, BigInteger change, string signTo);

        Task<IPendingOperation> GetOperationAsync(string operationId);
        Task<IPendingOperation> GetOperationByHashAsync(string hash);
        Task RefreshOperationAsync(string hash);
        Task RefreshOperationByIdAsync(string operationId);
        Task MatchHashToOpId(string transactionHash, string operationId);
        Task<string> CreateOperation(IPendingOperation operation);
    }

    public class PendingOperationService : IPendingOperationService
    {
        private class ReturnSignResult
        {
            public BigInteger Amount { get; set; }
            public string Sign { get; set; }
        }

        private readonly IBaseSettings _settings;
        private readonly IOperationToHashMatchRepository _operationToHashMatchRepository;
        private readonly IPendingOperationRepository _pendingOperationRepository;
        private readonly IQueueExt _queue;
        private readonly AddressUtil _util;
        private readonly Web3 _web3;
        private readonly IHashCalculator _hashCalculator;
        private readonly ICoinRepository _coinRepository;
        private readonly ILykkeSigningAPI _lykkeSigningAPI;
        private readonly ILog _log;
        private ISlackNotifier _slackNotifier;
        private readonly IEventTraceRepository _eventTraceRepository;

        public PendingOperationService(IBaseSettings settings, IOperationToHashMatchRepository operationToHashMatchRepository,
            IPendingOperationRepository pendingOperationRepository, IQueueFactory queueFactory, Web3 web3, IHashCalculator hashCalculator,
            ICoinRepository coinRepository, ILykkeSigningAPI lykkeSigningAPI, ILog log,
            ISlackNotifier slackNotifier, IEventTraceRepository eventTraceRepository)
        {
            _eventTraceRepository = eventTraceRepository;
            _slackNotifier = slackNotifier;
            _web3 = web3;
            _settings = settings;
            _pendingOperationRepository = pendingOperationRepository;
            _operationToHashMatchRepository = operationToHashMatchRepository;
            _queue = queueFactory.Build(Constants.PendingOperationsQueue);
            _util = new AddressUtil();
            _hashCalculator = hashCalculator;
            _coinRepository = coinRepository;
            _lykkeSigningAPI = lykkeSigningAPI;
            _log = log;
        }

        public async Task<string> CashOut(Guid id, string coinAddress, string fromAddress, string toAddress, BigInteger amount, string sign)
        {
            await ThrowOnExistingId(id);
            var coinAFromDb = await GetCoinWithCheck(coinAddress);
            var operation = new PendingOperation()
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
            };
            var opId = await CreateOperation(operation);

            var signResult = await GetAndCheckSign(id, coinAddress, fromAddress, toAddress, amount, sign);
            sign = signResult.Sign;
            amount = signResult.Amount;
            operation.SignFrom = sign;
            operation.Amount = amount.ToString();

            await StartProcessing(operation);

            return opId;
        }

        public async Task<string> Transfer(Guid id, string coinAddress, string fromAddress, string toAddress, BigInteger amount, string sign)
        {
            await ThrowOnExistingId(id);
            var coinAFromDb = await GetCoinWithCheck(coinAddress);
            var operation = new PendingOperation()
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
            };
            var opId = await CreateOperation(operation);

            var signResult = await GetAndCheckSign(id, coinAddress, fromAddress, toAddress, amount, sign);
            sign = signResult.Sign;
            amount = signResult.Amount;
            operation.SignFrom = sign;
            operation.Amount = amount.ToString();

            await StartProcessing(operation);

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
            var operation = new PendingOperation()
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
            };
            var opId = await CreateOperation(operation);

            var coinAFromDb = await GetCoinWithCheck(coinAddress);

            var signFromResult = await GetAndCheckSign(id, coinAddress, fromAddress, toAddress, amount, signFrom);
            signFrom = signFromResult.Sign;
            amount = signFromResult.Amount;

            var signToResult = await GetAndCheckSign(id, coinAddress, toAddress, fromAddress, change, signTo);
            signTo = signToResult.Sign;
            change = signToResult.Amount;

            operation.SignFrom = signFrom;
            operation.SignTo = signTo;
            operation.Change = change.ToString();
            operation.Amount = amount.ToString();

            await StartProcessing(operation);

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

        public async Task MatchHashToOpId(string transactionHash, string operationId)
        {
            IOperationToHashMatch match = await _operationToHashMatchRepository.GetAsync(operationId);
            if (match == null)
            {
                return;
            }

            match.TransactionHash = transactionHash;
            await _operationToHashMatchRepository.InsertOrReplaceAsync(match);
        }

        public async Task<IPendingOperation> GetOperationByHashAsync(string hash)
        {
            IOperationToHashMatch match = await _operationToHashMatchRepository.GetByHashAsync(hash);
            if (match == null)
            {
                return null;
            }

            IPendingOperation operation = await _pendingOperationRepository.GetOperation(match.OperationId);
            return operation;
        }

        public async Task RefreshOperationByIdAsync(string operationId)
        {
            IOperationToHashMatch match = await _operationToHashMatchRepository.GetAsync(operationId);
            if (match == null)
            {
                return;
            }

            match.TransactionHash = "";

            await _operationToHashMatchRepository.InsertOrReplaceAsync(match);
            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(new OperationHashMatchMessage() { OperationId = match.OperationId }));
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
            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(new OperationHashMatchMessage() { OperationId = match.OperationId }));
        }

        private async Task ThrowOnExistingId(Guid id)
        {
            var contract = _web3.Eth.GetContract(_settings.MainExchangeContract.Abi, _settings.MainExchangeContract.Address);
            var transactionsCheck = contract.GetFunction("transactions");
            var bigIntRepresentation = EthUtils.GuidToBigInteger(id);

            bool isInList = await transactionsCheck.CallAsync<bool>(bigIntRepresentation);
            var match = await _operationToHashMatchRepository.GetAsync(id.ToString());

            if (isInList || match != null)
            {
                throw new ClientSideException(ExceptionType.OperationWithIdAlreadyExists, $"operation with guid {id}");
            }
        }

        private async Task<ICoin> GetCoinWithCheck(string coinAddress)
        {
            var coin = await _coinRepository.GetCoinByAddress(coinAddress);

            if (coin == null)
            {
                throw new ClientSideException(ExceptionType.WrongParams, $"Coin with address {coinAddress}");
            }

            return coin;
        }

        private async Task<ReturnSignResult> GetAndCheckSign(Guid id, string coinAddress, string fromAddress, string toAddress, BigInteger amount, string signFrom)
        {
            bool isRobot = string.IsNullOrEmpty(signFrom);
            int retryCounter = 0;
            bool isSuccess = false;
            do
            {
                if (isRobot)
                {
                    signFrom = await GetSign(id, coinAddress, fromAddress, toAddress, amount);
                }

                try
                {
                    ThrowOnWrongSignature(id, coinAddress, fromAddress, toAddress, amount, signFrom);
                    isSuccess = true;
                }
                catch (ClientSideException exc)
                {
                    await _log.WriteErrorAsync("PendingOperationService", "GetAndCheckSign", $" OperationId {id} - Hash {signFrom}", exc, DateTime.UtcNow);
                    await _slackNotifier.ErrorAsync($"We recieved wrong signature! Sign can't be checked:  OperationId {id} - {signFrom} - {signFrom.Length}. Do something!");
                    throw;
                }
                catch (Exception e)
                {
                    await _log.WriteErrorAsync("PendingOperationService", "GetAndCheckSign", $" OperationId {id}", e, DateTime.UtcNow);
                    if (!isRobot)
                    {
                        throw;
                    }

                    retryCounter++;
                    amount++;
                    if (retryCounter > 1)
                    {
                        await _slackNotifier.ErrorAsync($"Dark Magic Happened! Sign can't be checked:  OperationId {id} - {signFrom} - {signFrom.Length}");
                        throw;
                    }

                    await _log.WriteInfoAsync("PendingOperationService", "GetAndCheckSign", $"ID:{id}, Adpater:{coinAddress}, From:{fromAddress}, To:{toAddress}, Amount:{amount}, IsRobotSignature:{isRobot}", "Retry with amount change! Amount here is more on 1 wei than original", DateTime.UtcNow);
                }
            } while (!isSuccess && (isRobot && retryCounter < 2));


            return new ReturnSignResult()
            {
                Amount = amount,
                Sign = signFrom,
            };
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

            try
            {
                var fixedSign = sign.EnsureHexPrefix();
                var hash = GetHash(id, coinAddress, clientAddr, toAddr, amount);
                var signer = new MessageSigner();
                string sender = signer.EcRecover(hash, sign);
                string checksumClientAddr = _util.ConvertToChecksumAddress(clientAddr);
                string checksumSender = _util.ConvertToChecksumAddress(sender);

                return checksumClientAddr == checksumSender;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private byte[] GetHash(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount)
        {
            return _hashCalculator.GetHash(id, coinAddress, clientAddr, toAddr, amount);
        }

        public async Task<string> CreateOperation(IPendingOperation operation)
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
                Change = operation.Change
            };

            var match = new OperationToHashMatch()
            {
                OperationId = op.OperationId,
                TransactionHash = ""
            };

            await _operationToHashMatchRepository.InsertOrReplaceAsync(match);
            await _pendingOperationRepository.InsertOrReplace(op);

            return op.OperationId;
        }

        private async Task StartProcessing(IPendingOperation operation)
        {
            await CreateOperation(operation);
            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(new OperationHashMatchMessage() { OperationId = operation.OperationId }));
            await _eventTraceRepository.InsertAsync(new EventTrace() { Note = $"First appearance for the operation. Put it in{Constants.PendingOperationsQueue}",
                OperationId = operation.OperationId, TraceDate = DateTime.UtcNow });
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

            await _log.WriteInfoAsync("PendingOperationService", "GetSign", "", $"ID({id})-COINADDRESS({coinAddress})-FROM({clientAddr})-TO({toAddr})-AMOUNT({amount})-HASH({response.SignedHash})", DateTime.UtcNow);
            return response.SignedHash;
        }
    }
}

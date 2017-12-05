using Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Core.Settings;
using Core.Repositories;
using Core.Messages.HotWallet;
using Services.PrivateWallet;
using Services.Signature;
using Common.Log;
using Nethereum.Web3;
using System.Numerics;
using Core.Exceptions;
using Services.Coins.Models;
using Common;
using System.Threading;
using System.Collections.Concurrent;

namespace Services.HotWallet
{
    public class HotWalletService : IHotWalletService
    {
        private readonly IQueueExt _hotWalletTransactionMonitoringQueue;
        private readonly IQueueExt _hotWalletCashoutQueue;
        private readonly IBaseSettings _baseSettings;
        private readonly IHotWalletOperationRepository _hotWalletCashoutRepository;
        private readonly IPrivateWalletService _privateWalletService;
        private readonly IErc20PrivateWalletService _erc20PrivateWalletService;
        private readonly ILog _log;
        private readonly Web3 _web3;
        private readonly BigInteger _maxGasPrice;
        private readonly BigInteger _minGasPrice;
        private readonly IHotWalletTransactionRepository _hotWalletCashoutTransactionRepository;
        private readonly ISignatureService _signatureService;
        private readonly IErc20DepositContractService _erc20DepositContractService;
        private readonly SettingsWrapper _settingsWrapper;
        private readonly IUserTransferWalletRepository _userTransferWalletRepository;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores;

        public HotWalletService(IBaseSettings baseSettings,
            IQueueFactory queueFactory,
            IHotWalletOperationRepository hotWalletCashoutRepository,
            IPrivateWalletService privateWalletService,
            IErc20PrivateWalletService erc20PrivateWalletService,
            ISignatureService signatureService,
            ILog log,
            Web3 web3,
            IHotWalletTransactionRepository hotWalletCashoutTransactionRepository,
            IErc20DepositContractService erc20DepositContractService,
            SettingsWrapper settingsWrapper,
            IUserTransferWalletRepository userTransferWalletRepository)
        {
            _hotWalletTransactionMonitoringQueue = queueFactory.Build(Constants.HotWalletTransactionMonitoringQueue);
            _hotWalletCashoutQueue = queueFactory.Build(Constants.HotWalletCashoutQueue);
            _baseSettings = baseSettings;//.HotWalletAddress
            _hotWalletCashoutRepository = hotWalletCashoutRepository;
            _privateWalletService = privateWalletService;
            _erc20PrivateWalletService = erc20PrivateWalletService;
            _log = log;
            _web3 = web3;
            _maxGasPrice = new BigInteger(_baseSettings.MaxGasPrice);
            _minGasPrice = new BigInteger(_baseSettings.MinGasPrice);
            _hotWalletCashoutTransactionRepository = hotWalletCashoutTransactionRepository;
            _signatureService = signatureService;
            _erc20DepositContractService = erc20DepositContractService;
            _settingsWrapper = settingsWrapper;
            _userTransferWalletRepository = userTransferWalletRepository;
            _semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        public async Task EnqueueCashoutAsync(IHotWalletOperation hotWalletCashout)
        {
            if (hotWalletCashout == null)
            {
                return;
            }

            var existingCashout = await _hotWalletCashoutRepository.GetAsync(hotWalletCashout.OperationId);

            if (existingCashout != null)
            {
                throw new ClientSideException(ExceptionType.EntityAlreadyExists, "Operation with Id was enqueued before");
            }

            hotWalletCashout.OperationType = HotWalletOperationType.Cashout;
            await RetryCashoutAsync(hotWalletCashout);
        }

        public async Task RetryCashoutAsync(IHotWalletOperation hotWalletCashout)
        {
            HotWalletCashoutMessage message = new HotWalletCashoutMessage() { OperationId = hotWalletCashout.OperationId };

            await _hotWalletCashoutRepository.SaveAsync(hotWalletCashout);
            await _hotWalletCashoutQueue.PutRawMessageAsync(Newtonsoft.Json.JsonConvert.SerializeObject(message));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hotWalletCashout"></param>
        /// <returns>transaction hash</returns>
        public async Task<string> StartCashoutAsync(string operationId)
        {
            IHotWalletOperation cashout = await _hotWalletCashoutRepository.GetAsync(operationId);

            if (cashout == null || cashout.OperationType != HotWalletOperationType.Cashout)
            {
                await _log.WriteWarningAsync(nameof(HotWalletService),
                    nameof(StartCashoutAsync),
                    $"operationId - {operationId}",
                    "No cashout info for operation",
                    DateTime.UtcNow);

                return null;
            }

            var currentGasPriceHex = await _web3.Eth.GasPrice.SendRequestAsync();
            var currentGasPrice = currentGasPriceHex.Value;
            BigInteger selectedGasPrice = currentGasPrice * _baseSettings.GasPricePercentage / 100;
            if (selectedGasPrice > _maxGasPrice)
            {
                selectedGasPrice = _maxGasPrice;
            }
            else if (selectedGasPrice < _minGasPrice)
            {
                selectedGasPrice = _minGasPrice;
            }

            string transactionForSigning = null;
            string signedTransaction = null;
            string transactionHash = null;
            bool isErc20Transfer = !string.IsNullOrEmpty(cashout.TokenAddress);
            SemaphoreSlim semaphore = _semaphores.GetOrAdd(cashout.FromAddress, f => new SemaphoreSlim(1, 1));

            try
            {
                await semaphore.WaitAsync();
                //Erc20 transfer
                if (isErc20Transfer)
                {
                    transactionForSigning = await _erc20PrivateWalletService.GetTransferTransactionRaw(new BusinessModels.PrivateWallet.Erc20Transaction()
                    {
                        FromAddress = cashout.FromAddress,
                        GasAmount = Constants.GasForCoinTransaction,
                        GasPrice = selectedGasPrice,
                        ToAddress = cashout.ToAddress,
                        TokenAddress = cashout.TokenAddress,
                        TokenAmount = cashout.Amount,
                        Value = 0,
                    }, useTxPool: true);
                }
                //Eth transfer
                else
                {
                    transactionForSigning = await _privateWalletService.GetTransactionForSigning(new BusinessModels.PrivateWallet.EthTransaction()
                    {
                        FromAddress = cashout.FromAddress,
                        GasAmount = Constants.GasForCoinTransaction,
                        GasPrice = selectedGasPrice,
                        ToAddress = cashout.ToAddress,
                        Value = cashout.Amount
                    }, useTxPool: true);
                }

                signedTransaction = await _signatureService.SignRawTransactionAsync(cashout.FromAddress, transactionForSigning);

                if (string.IsNullOrEmpty(signedTransaction))
                {
                    throw new ClientSideException(ExceptionType.WrongSign, "Wrong signature");
                }

                var transactionExecutionCosts =
                    await _privateWalletService.EstimateTransactionExecutionCost(cashout.FromAddress, signedTransaction);

                if (!transactionExecutionCosts.IsAllowed)
                {
                    throw new Exception($"Transaction will not be successfull {cashout.ToJson()}");
                }

                transactionHash = isErc20Transfer ? await _erc20PrivateWalletService.SubmitSignedTransaction(cashout.FromAddress, signedTransaction) :
                    await _privateWalletService.SubmitSignedTransaction(cashout.FromAddress, signedTransaction);
            }
            finally
            {
                semaphore.Release();
            }

            if (string.IsNullOrEmpty(transactionHash))
            {
                throw new Exception("Transaction was not sent");
            }

            CoinTransactionMessage message = new CoinTransactionMessage()
            {
                OperationId = operationId,
                TransactionHash = transactionHash
            };

            await _hotWalletCashoutTransactionRepository.SaveAsync(new HotWalletCashoutTransaction()
            {
                OperationId = operationId,
                TransactionHash = transactionHash
            });
            await _hotWalletTransactionMonitoringQueue.PutRawMessageAsync(Newtonsoft.Json.JsonConvert.SerializeObject(message));

            return transactionHash;
        }

        public async Task SaveOperationAsync(IHotWalletOperation operation)
        {
            await _hotWalletCashoutRepository.SaveAsync(operation);
        }

        public async Task<string> StartCashinAsync(IHotWalletOperation operation)
        {
            await SaveOperationAsync(operation);
            var transactionHash = await _erc20DepositContractService.RecievePaymentFromDepositContract(operation.FromAddress,
                operation.TokenAddress,
                operation.ToAddress);

            await _hotWalletCashoutTransactionRepository.SaveAsync(new HotWalletCashoutTransaction()
            {
                OperationId = operation.OperationId,
                TransactionHash = transactionHash
            });

            CoinTransactionMessage message = new CoinTransactionMessage()
            {
                OperationId = operation.OperationId,
                TransactionHash = transactionHash
            };

            return transactionHash;
        }

        public async Task RemoveCashinLockAsync(string erc20TokenAddress, string contractAddress)
        {
            string userAddress = await _erc20DepositContractService.GetUserAddress(contractAddress);
            await UpdateUserTransferWallet(contractAddress, erc20TokenAddress, userAddress);
        }

        private async Task UpdateUserTransferWallet(string contractAddress, string erc20TokenAddress, string userAddress)
        {
            string formattedAddress =
                _userTransferWalletRepository.FormatAddressForErc20(contractAddress, erc20TokenAddress);

            await _userTransferWalletRepository.ReplaceAsync(new UserTransferWallet()
            {
                LastBalance = "",
                TransferContractAddress = formattedAddress,
                UpdateDate = DateTime.UtcNow,
                UserAddress = userAddress
            });
        }
    }
}

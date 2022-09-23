﻿using Lykke.Service.EthereumCore.Core;
using System;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Messages.HotWallet;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using Lykke.Service.EthereumCore.Services.Signature;
using Common.Log;
using Nethereum.Web3;
using System.Numerics;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Services.Coins.Models;
using Common;
using System.Threading;
using System.Collections.Concurrent;
using Autofac.Features.AttributeFilters;
using Lykke.Common.Log;
using Lykke.Service.EthereumCore.Core.Shared;
using Lykke.Service.EthereumCore.Core.Utils;

namespace Lykke.Service.EthereumCore.Services.HotWallet
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
        private readonly IHotWalletTransactionRepository _hotWalletCashoutTransactionRepository;
        private readonly ISignatureService _signatureService;
        private readonly IErc20DepositContractService _erc20DepositContractService;
        private readonly AppSettings _settingsWrapper;
        private readonly IUserTransferWalletRepository _userTransferWalletRepository;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores;
        private readonly IGasPriceRepository _gasPriceRepository;

        public HotWalletService(IBaseSettings baseSettings,
            IQueueFactory queueFactory,
            IHotWalletOperationRepository hotWalletCashoutRepository,
            IPrivateWalletService privateWalletService,
            IErc20PrivateWalletService erc20PrivateWalletService,
            ISignatureService signatureService,
            ILog log,
            Web3 web3,
            IHotWalletTransactionRepository hotWalletCashoutTransactionRepository,
            [KeyFilter(Constants.DefaultKey)]IErc20DepositContractService erc20DepositContractService,
            AppSettings settingsWrapper,
            IUserTransferWalletRepository userTransferWalletRepository,
            IGasPriceRepository gasPriceRepository)
        {
            _hotWalletTransactionMonitoringQueue = queueFactory.Build(Constants.HotWalletTransactionMonitoringQueue);
            _hotWalletCashoutQueue = queueFactory.Build(Constants.HotWalletCashoutQueue);
            _baseSettings = baseSettings;//.HotWalletAddress
            _hotWalletCashoutRepository = hotWalletCashoutRepository;
            _privateWalletService = privateWalletService;
            _erc20PrivateWalletService = erc20PrivateWalletService;
            _log = log;
            _web3 = web3;
            _hotWalletCashoutTransactionRepository = hotWalletCashoutTransactionRepository;
            _signatureService = signatureService;
            _erc20DepositContractService = erc20DepositContractService;
            _settingsWrapper = settingsWrapper;
            _userTransferWalletRepository = userTransferWalletRepository;
            _semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            _gasPriceRepository = gasPriceRepository;
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

            var gasPrice = await _gasPriceRepository.GetAsync();
            var currentGasPriceHex = await _web3.Eth.GasPrice.SendRequestAsync();
            var currentGasPrice = currentGasPriceHex.Value;
            BigInteger selectedGasPrice = currentGasPrice * _baseSettings.GasPricePercentage / 100;
            if (selectedGasPrice > gasPrice.Max)
            {
                selectedGasPrice = gasPrice.Max;
            }
            else if (selectedGasPrice < gasPrice.Min)
            {
                selectedGasPrice = gasPrice.Min;
            }

            string transactionForSigning = null;
            string signedTransaction = null;
            string transactionHash = null;
            bool isErc20Transfer = !string.IsNullOrEmpty(cashout.TokenAddress);
            SemaphoreSlim semaphore = _semaphores.GetOrAdd(cashout.FromAddress, f => new SemaphoreSlim(1, 1));
            
            _log.Info("Obtaining transaction for signing", new
            {
                FromAddress = cashout.FromAddress,
                GasAmount = _baseSettings.GasForHotWalletTransaction,
                GasPrice = selectedGasPrice,
                ToAddress = cashout.ToAddress,
                TokenAddress = cashout.TokenAddress,
                TokenAmount = cashout.Amount
            });

            try
            {
                await semaphore.WaitAsync();
                //Erc20 transfer
                if (isErc20Transfer)
                {
                    transactionForSigning = await _erc20PrivateWalletService.GetTransferTransactionRaw(new Lykke.Service.EthereumCore.BusinessModels.PrivateWallet.Erc20Transaction()
                    {
                        FromAddress = cashout.FromAddress,
                        GasAmount = _baseSettings.GasForHotWalletTransaction,
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
                    transactionForSigning = await _privateWalletService.GetTransactionForSigning(new Lykke.Service.EthereumCore.BusinessModels.PrivateWallet.EthTransaction()
                    {
                        FromAddress = cashout.FromAddress,
                        GasAmount = _baseSettings.GasForHotWalletTransaction,
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
                    throw new Exception($"Transaction will not be successfull {JsonSerialisersExt.ToJson(cashout)}");
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

            await RetryPolicy.ExecuteUnlimitedAsync(async () =>
            {
                await _hotWalletCashoutTransactionRepository.SaveAsync(new HotWalletCashoutTransaction()
                {
                    OperationId = operation.OperationId,
                    TransactionHash = transactionHash
                });
            }, TimeSpan.FromMinutes(1).Milliseconds, _log);

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
            await TransferWalletSharedService.UpdateUserTransferWalletAsync(_userTransferWalletRepository, contractAddress,
                erc20TokenAddress, userAddress, "");
        }
    }
}

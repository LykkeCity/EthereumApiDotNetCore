using System;
using System.Numerics;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Newtonsoft.Json;
using Lykke.Service.EthereumCore.Services.Coins.Models;
using Common.Log;
using AzureStorage.Queue;

namespace Lykke.Service.EthereumCore.Services.Coins
{
    public interface ICoinTransactionService
    {
        Task<ICoinTransaction> ProcessTransaction(CoinTransactionMessage transaction);
        Task PutTransactionToQueue(string transactionHash, string operationId);
    }
    public class CoinTransactionService : ICoinTransactionService
    {
        public const int Level1Confirm = 1;
        public const int Level2Confirm = 2;
        public const int Level3Confirm = 3;

        private readonly IEthereumTransactionService _transactionService;
        private readonly ICoinTransactionRepository _coinTransactionRepository;
        private readonly IContractService _contractService;
        private readonly IBaseSettings _baseSettings;
        private readonly ILog _logger;
        private readonly IQueueExt _coinTransationMonitoringQueue;
        private readonly IPendingTransactionsRepository _pendingTransactionsRepository;

        public CoinTransactionService(Func<string, IQueueExt> queueFactory, IEthereumTransactionService transactionService,
            ICoinTransactionRepository coinTransactionRepository, IContractService contractService, IBaseSettings baseSettings, ILog logger,
            IPendingTransactionsRepository pendingTransactionsRepository)
        {
            _transactionService = transactionService;
            _coinTransactionRepository = coinTransactionRepository;
            _contractService = contractService;
            _baseSettings = baseSettings;
            _logger = logger;
            _coinTransationMonitoringQueue = queueFactory(Constants.TransactionMonitoringQueue);
            _pendingTransactionsRepository = pendingTransactionsRepository;
        }


        public async Task<ICoinTransaction> ProcessTransaction(CoinTransactionMessage transaction)
        {
            var receipt = await _transactionService.GetTransactionReceipt(transaction.TransactionHash);
            if (receipt == null)
                return null;

            ICoinTransaction coinDbTransaction = await _coinTransactionRepository.GetTransaction(transaction.TransactionHash)
                ?? new CoinTransaction()
                {
                    ConfirmationLevel = 0,
                    TransactionHash = transaction.TransactionHash
                };
            bool error = coinDbTransaction?.Error == true || !await _transactionService.IsTransactionExecuted(transaction.TransactionHash);

            var confimations = await _contractService.GetCurrentBlock() - receipt.BlockNumber;
            var coinTransaction = new CoinTransaction
            {
                TransactionHash = transaction.TransactionHash,
                Error = error,
                ConfirmationLevel = GetTransactionConfirmationLevel(confimations)
            };

            await _coinTransactionRepository.InsertOrReplaceAsync(coinTransaction);

            return coinTransaction;
        }

        private int GetTransactionConfirmationLevel(BigInteger confimations)
        {
            if (confimations >= _baseSettings.Level3TransactionConfirmation)
                return Level3Confirm;
            if (confimations >= _baseSettings.Level2TransactionConfirmation)
                return Level2Confirm;
            if (confimations >= _baseSettings.Level1TransactionConfirmation)
                return Level1Confirm;
            return 0;
        }

        public Task PutTransactionToQueue(string transactionHash, string operationId)
        {
            return PutTransactionToQueue(new CoinTransactionMessage
            {
                TransactionHash = transactionHash,
                OperationId = operationId,
                PutDateTime = DateTime.UtcNow
            });
        }

        public async Task PutTransactionToQueue(CoinTransactionMessage transaction)
        {
            await _coinTransationMonitoringQueue.PutRawMessageAsync(JsonConvert.SerializeObject(transaction));
        }
    }
}

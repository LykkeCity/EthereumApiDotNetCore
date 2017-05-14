using System;
using System.Numerics;
using System.Threading.Tasks;
using Core;
using Core.Repositories;
using Core.Settings;
using Newtonsoft.Json;
using Services.Coins.Models;
using Common.Log;
using AzureStorage.Queue;

namespace Services.Coins
{
    public interface ICoinTransactionService
    {
        Task<bool> ProcessTransaction(CoinTransactionMessage transaction);
        Task PutTransactionToQueue(string transactionHash);
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
        private readonly IQueueExt _coinTransactionQueue;

        public CoinTransactionService(Func<string, IQueueExt> queueFactory, IEthereumTransactionService transactionService,
            ICoinTransactionRepository coinTransactionRepository, IContractService contractService, IBaseSettings baseSettings, ILog logger)
        {
            _transactionService = transactionService;
            _coinTransactionRepository = coinTransactionRepository;
            _contractService = contractService;
            _baseSettings = baseSettings;
            _logger = logger;
            _coinTransationMonitoringQueue = queueFactory(Constants.TransactionMonitoringQueue);
            _coinTransactionQueue = queueFactory(Constants.CoinTransactionQueue);
        }


        public async Task<bool> ProcessTransaction(CoinTransactionMessage transaction)
        {
            var receipt = await _transactionService.GetTransactionReceipt(transaction.TransactionHash);
            if (receipt == null)
                return false;

            ICoinTransaction coinDbTransaction = await _coinTransactionRepository.GetTransaction(transaction.TransactionHash) 
                ?? new CoinTransaction()
                    {
                        ConfirmationLevel = 0,
                        TransactionHash = transaction.TransactionHash
                    };
            bool error = coinDbTransaction?.Error == true || !await _transactionService.IsTransactionExecuted(transaction.TransactionHash, Constants.GasForCoinTransaction);

            var confimations = await _contractService.GetCurrentBlock() - receipt.BlockNumber;
            var coinTransaction = new CoinTransaction
            {
                TransactionHash = transaction.TransactionHash,
                Error = error,
                ConfirmationLevel = GetTransactionConfirmationLevel(confimations)
            };

            await _coinTransactionRepository.InsertOrReplaceAsync(coinTransaction);

            if (!error && coinTransaction.ConfirmationLevel != Level3Confirm)
            {
                await PutTransactionToQueue(coinTransaction.TransactionHash);
                if (coinTransaction.ConfirmationLevel != coinDbTransaction?.ConfirmationLevel)
                    await _logger.WriteInfoAsync("CoinTransactionService", "ProcessTransaction", "",
                            $"Put coin transaction {coinTransaction.TransactionHash} to monitoring queue with confimation level {coinTransaction.ConfirmationLevel}");
            }

            await FireTransactionCompleteEvent(coinTransaction, coinDbTransaction);
            return true;
        }

        private async Task FireTransactionCompleteEvent(CoinTransaction coinTransaction, ICoinTransaction coinDbTransaction)
        {
            if (coinTransaction.ConfirmationLevel != (coinDbTransaction?.ConfirmationLevel ?? 0) ||
                coinTransaction.Error != coinDbTransaction?.Error)
            {
                await _coinTransactionQueue.PutRawMessageAsync(JsonConvert.SerializeObject(new CoinTransactionCompleteEvent
                {
                    TransactionHash = coinTransaction.TransactionHash,
                    ConfirmationLevel = coinTransaction.ConfirmationLevel,
                    Error = coinTransaction.Error
                }));

                await _logger.WriteInfoAsync("CoinTransactionService", "ProcessTransaction", "",
                    $"Put coin transaction {coinTransaction.TransactionHash} to finished queue with confimation level {coinTransaction.ConfirmationLevel}. Error = {coinTransaction.Error}");
            }
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

        public Task PutTransactionToQueue(string transactionHash)
        {
            return PutTransactionToQueue(new CoinTransactionMessage { TransactionHash = transactionHash });
        }

        public async Task PutTransactionToQueue(CoinTransactionMessage transaction)
        {
            await _coinTransationMonitoringQueue.PutRawMessageAsync(JsonConvert.SerializeObject(transaction));
        }
    }
}

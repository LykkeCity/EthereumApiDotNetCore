using System;
using System.Threading.Tasks;
using Services.Coins;
using Common.Log;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;
using Core;
using Services.Coins.Models;
using Lykke.JobTriggers.Triggers.Bindings;
using Core.Settings;
using Core.Notifiers;
using Core.Repositories;
using Services;

namespace EthereumJobs.Job
{
    public class MonitoringExchangeTransactionJob
    {
        private readonly ILog _log;
        private readonly ICoinTransactionService _coinTransactionService;
        private readonly IBaseSettings _settings;
        private readonly ISlackNotifier _slackNotifier;
        private readonly ICoinEventService _coinEventService;
        private readonly IPendingTransactionsRepository _pendingTransactionsRepository;

        public MonitoringExchangeTransactionJob(ILog log, ICoinTransactionService coinTransactionService, 
            IBaseSettings settings, ISlackNotifier slackNotifier, ICoinEventService coinEventService, 
            IPendingTransactionsRepository pendingTransactionsRepository)
        {
            _settings = settings;
            _log = log;
            _coinTransactionService = coinTransactionService;
            _slackNotifier = slackNotifier;
            _coinEventService = coinEventService;
            _pendingTransactionsRepository = pendingTransactionsRepository;
        }

        [QueueTrigger(Constants.TransactionMonitoringQueue, 100, true)]
        public async Task Execute(CoinTransactionMessage transaction, QueueTriggeringContext context)
        {
            ICoinTransaction coinTransaction = null;
            try
            {
                coinTransaction = await _coinTransactionService.ProcessTransaction(transaction);
            }
            catch (Exception ex)
            {
                if (ex.Message != transaction.LastError)
                    await _log.WriteWarningAsync("MonitoringCoinTransactionJob", "Execute", $"TrHash: [{transaction.TransactionHash}]", "");

                transaction.LastError = ex.Message;

                if (transaction.DequeueCount >= _settings.MaxDequeueCount)
                {
                    context.MoveMessageToPoison();
                }
                else
                {
                    transaction.DequeueCount++;
                    context.MoveMessageToEnd(transaction.ToJson());
                    context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, 200);
                }

                await _log.WriteErrorAsync("MonitoringCoinTransactionJob", "Execute", "", ex);
                return;
            }

            if ((coinTransaction == null || (coinTransaction.Error || coinTransaction.ConfirmationLevel == 0)) && 
                DateTime.UtcNow - transaction.PutDateTime > TimeSpan.FromSeconds(_settings.BroadcastMonitoringPeriodSeconds))
            {
                context.MoveMessageToPoison(transaction.ToJson());
                await _slackNotifier.ErrorAsync($"EthereumCoreService: Transaction with hash {transaction.TransactionHash} has no confirmations. Reason - timeout");
                await SendCompletedCoinEvent(transaction.TransactionHash, false);
            }
            else
            {
                if (coinTransaction != null && coinTransaction.ConfirmationLevel != 0)
                {
                    await SendCompletedCoinEvent(transaction.TransactionHash, true);
                    await _log.WriteInfoAsync("CoinTransactionService", "Execute", "",
                               $"Put coin transaction {transaction.TransactionHash} to monitoring queue with confimation level {coinTransaction?.ConfirmationLevel ?? 0}");
                }
                else
                { 
                    context.MoveMessageToEnd(transaction.ToJson());
                    context.SetCountQueueBasedDelay(10000, 100);
                        await _log.WriteInfoAsync("CoinTransactionService", "Execute", "",
                                $"Put coin transaction {transaction.TransactionHash} to monitoring queue with confimation level {coinTransaction?.ConfirmationLevel ?? 0}");
                }
                
            }
        }

        private async Task SendCompletedCoinEvent(string transactionHash, bool success)
        {
            var coinEvent = await _coinEventService.GetCoinEvent(transactionHash);
            coinEvent.Success = success;

            switch (coinEvent.CoinEventType)
            {
                case CoinEventType.CashinStarted:
                case CoinEventType.CashoutStarted:
                case CoinEventType.TransferStarted:
                    //Say that Event Is completed
                    coinEvent.CoinEventType++;
                    break;
                default: break;
            }

            await _pendingTransactionsRepository.Delete(transactionHash);
            await _coinEventService.PublishEvent(coinEvent, false);
        }
    }
}

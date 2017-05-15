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

namespace EthereumJobs.Job
{
    public class MonitoringCoinTransactionJob
    {
        private readonly ILog _log;
        private readonly ICoinTransactionService _coinTransactionService;
        private readonly IBaseSettings _settings;
        private readonly ISlackNotifier _slackNotifier;

        public MonitoringCoinTransactionJob(ILog log, ICoinTransactionService coinTransactionService, IBaseSettings settings, ISlackNotifier slackNotifier)
        {
            _settings = settings;
            _log = log;
            _coinTransactionService = coinTransactionService;
            _slackNotifier = slackNotifier;
        }

        [QueueTrigger(Constants.TransactionMonitoringQueue, 100, true)]
        public async Task Execute(CoinTransactionMessage transaction, QueueTriggeringContext context)
        {
            ICoinTransaction coinTransaction = null;
            try
            {
                coinTransaction = await _coinTransactionService.ProcessTransaction(transaction);
                if (coinTransaction == null)
                {
                    //TODO:Fire transaction failed
                    return;
                }
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
                await _log.WriteErrorAsync("EthereumJob", "MonitoringCoinTransactionJob", "", ex);
            }
            if (coinTransaction.ConfirmationLevel != 3 && DateTime.UtcNow - transaction.PutDateTime > TimeSpan.FromSeconds(_settings.BroadcastMonitoringPeriodSeconds))
            {
                context.MoveMessageToPoison(transaction.ToJson());
                await _slackNotifier.ErrorAsync($"EthereumCoreService: Transaction with hash {transaction.TransactionHash} has no confirmations");
            }
            else
            {
                //IF coinTransaction.ConfirmationLevel == 3 send event to external services via rabbit
                if (!coinTransaction.Error && coinTransaction.ConfirmationLevel != 3)
                {
                    context.MoveMessageToEnd(transaction.ToJson());
                    context.SetCountQueueBasedDelay(10000, 100);
                        await _log.WriteInfoAsync("CoinTransactionService", "ProcessTransaction", "",
                                $"Put coin transaction {coinTransaction.TransactionHash} to monitoring queue with confimation level {coinTransaction.ConfirmationLevel}");
                }
                
            }
        }
    }
}

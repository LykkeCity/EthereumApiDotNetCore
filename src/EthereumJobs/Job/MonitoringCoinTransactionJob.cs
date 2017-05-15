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

namespace EthereumJobs.Job
{
    public class MonitoringCoinTransactionJob
    {
        private readonly ILog _log;
        private readonly ICoinTransactionService _coinTransactionService;
        private readonly IBaseSettings _settings;

        public MonitoringCoinTransactionJob(ILog log, ICoinTransactionService coinTransactionService, IBaseSettings settings)
        {
            _settings = settings;
            _log = log;
            _coinTransactionService = coinTransactionService;
        }

        //[QueueTrigger(Constants.TransactionMonitoringQueue, 200, true)]
        public async Task Execute(CoinTransactionMessage transaction, QueueTriggeringContext context)
        {
            try
            {
                await _coinTransactionService.ProcessTransaction(transaction);
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
        }
    }
}

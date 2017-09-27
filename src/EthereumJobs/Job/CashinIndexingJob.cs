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
using Newtonsoft.Json;
using Services.New;
using Services.New.Models;

namespace EthereumJobs.Job
{
    public class CashinIndexingJob
    {
        private readonly ILog _log;
        private readonly ICoinEventService _coinEventService;
        private ICoinTransactionService _coinTransactionService;
        private readonly IBaseSettings _settings;
        private readonly ICoinRepository _coinRepository;
        private readonly ITransactionEventsService _transactionEventsService;

        public CashinIndexingJob(ILog log, IBaseSettings settings, 
            ITransactionEventsService transactionEventsService,
            ICoinRepository coinRepository,
            ICoinEventService coinEventService,
            ICoinTransactionService coinTransactionService)
        {
            _coinRepository = coinRepository;
            _transactionEventsService = transactionEventsService;
            _settings = settings;
            _log = log;
            _coinEventService = coinEventService;
            _coinTransactionService = coinTransactionService;
        }

        [TimerTrigger("0.00:00:30")]
        public async Task Execute()
        {
            try
            {
                await _coinRepository.ProcessAllAsync(async (adapters) =>
                {
                    foreach (var adapter in adapters)
                    {
                        await _log.WriteInfoAsync("CashinIndexingJob", "Execute",
                            $"Coin adapter address{adapter.AdapterAddress}",
                            "Cashin Indexing has been started", DateTime.UtcNow);

                        await _transactionEventsService.IndexCashinEvents(adapter.AdapterAddress, adapter.DeployedTransactionHash);
                    }
                });
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync("MonitoringCoinTransactionJob", "Execute", "", ex);
                return;
            }
        }

        [QueueTrigger(Constants.CashinCompletedEventsQueue)]
        public async Task ExecuteCashinCompleted(CoinEventCashinCompletedMessage message, QueueTriggeringContext context)
        {
            try
            {
                if (message == null || string.IsNullOrEmpty(message.TransactionHash))
                {
                    context.MoveMessageToPoison(message?.ToJson());
                }

                var coinEvent = await _coinEventService.GetCoinEvent(message.TransactionHash);

                if (coinEvent == null)
                {
                    return;
                }

                if (coinEvent.CoinEventType == CoinEventType.CashinStarted)
                {
                    await _coinTransactionService.PutTransactionToQueue(coinEvent.TransactionHash, coinEvent.OperationId);
                }
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync("MonitoringCoinTransactionJob", "Execute", "", ex);

                if (message.DequeueCount > 10000)
                {
                    context.MoveMessageToPoison(message.ToJson());
                    return;
                }

                context.MoveMessageToEnd(message.ToJson());
                context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, 150);
            }
        }
    }
}

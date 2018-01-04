using System;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Services.Coins;
using Common.Log;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Services.Coins.Models;
using Lykke.JobTriggers.Triggers.Bindings;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Core.Notifiers;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Services;
using Newtonsoft.Json;
using Lykke.Service.EthereumCore.Services.New;
using Lykke.Service.EthereumCore.Services.New.Models;

namespace Lykke.Job.EthereumCore.Job
{
    public class CashinIndexingJob
    {
        private readonly ILog _log;
        private readonly ICoinEventService _coinEventService;
        private readonly ICoinTransactionService _coinTransactionService;
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
        public async Task ExecuteForAdapters()
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

                        await _transactionEventsService.IndexCashinEventsForAdapter(adapter.AdapterAddress, adapter.DeployedTransactionHash);
                    }
                });
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(CashinIndexingJob), nameof(ExecuteForAdapters), "", ex);
            }
        }

        [TimerTrigger("0.00:00:30")]
        public async Task ExecuteForErc20Deposits()
        {
            try
            {
                await _transactionEventsService.IndexCashinEventsForErc20Deposits();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(CashinIndexingJob), nameof(ExecuteForAdapters), "", ex);
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

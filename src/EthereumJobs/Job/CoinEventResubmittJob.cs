using System;
using System.Numerics;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core;
using Core.Repositories;
using Core.Settings;
using EdjCase.JsonRpc.Client;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;
using Services;
using Services.Coins;
using Services.New.Models;
using Core.Exceptions;
using Services.Coins.Models;
using AzureStorage.Queue;
using System.Linq;
using Core.Notifiers;

namespace EthereumJobs.Job
{
    public class CoinEventResubmittJob
    {
        private readonly ICoinEventService _coinEventService;
        private readonly IEventTraceRepository _eventTraceRepository;
        private readonly IExchangeContractService _exchangeContractService;
        private readonly ILog _log;
        private readonly IPendingOperationService _pendingOperationService;
        private readonly IBaseSettings _settings;
        private readonly ITransferContractService _transferContractService;
        private readonly IEthereumTransactionService _ethereumTransactionService;
        private readonly IQueueExt _transactionMonitoringQueue;
        private readonly ISlackNotifier _slackNotifier;
        private IOperationResubmittRepository _operationResubmittRepository;

        public CoinEventResubmittJob(
            ILog log,
            IBaseSettings settings,
            IPendingOperationService pendingOperationService,
            IExchangeContractService exchangeContractService,
            ICoinEventService coinEventService,
            ITransferContractService transferContractService,
            IEventTraceRepository eventTraceRepository,
            IEthereumTransactionService ethereumTransactionService,
            IQueueFactory queueFactory,
            ISlackNotifier slackNotifier,
            IOperationResubmittRepository operationResubmittRepository)
        {
            _eventTraceRepository = eventTraceRepository;
            _exchangeContractService = exchangeContractService;
            _pendingOperationService = pendingOperationService;
            _settings = settings;
            _log = log;
            _coinEventService = coinEventService;
            _transferContractService = transferContractService;
            _ethereumTransactionService = ethereumTransactionService;
            _transactionMonitoringQueue = queueFactory.Build(Constants.TransactionMonitoringQueue);
            _slackNotifier = slackNotifier;
            _operationResubmittRepository = operationResubmittRepository;
        }

        [QueueTrigger(Constants.CoinEventResubmittQueue, 100, true)]
        public async Task Execute(OperationHashMatchMessage opMessage, QueueTriggeringContext context)
        {
            try
            {
                string operatioId = opMessage.OperationId;
                IOperationResubmitt opResubmitCounter = await _operationResubmittRepository.GetAsync(operatioId);
                if (opResubmitCounter == null)
                {
                    opResubmitCounter = new OperationResubmitt()
                    {
                        OperationId = operatioId,
                        ResubmittCount = 0
                    };
                }
                if (opResubmitCounter.ResubmittCount > 2)
                {
                    await _log.WriteWarningAsync("CoinEventResubmittJob", "Execute", "", $"Message put to poison {opMessage.OperationId}");
                    context.MoveMessageToPoison(opMessage.ToJson());
                    
                    return;
                }

                var historicalMessages = await _pendingOperationService.GetHistoricalAsync(opMessage.OperationId);
                if (historicalMessages == null || historicalMessages.Count() == 0)
                {
                    //Process cashin operations
                    var coinEvent = await _coinEventService.GetCoinEventById(opMessage.OperationId);
                    if (coinEvent != null &&
                        await _ethereumTransactionService.IsTransactionExecuted(coinEvent.TransactionHash, Constants.GasForCoinTransaction))
                    {
                        await ResubmittTransactionAsync(coinEvent.TransactionHash, operatioId, opResubmitCounter);

                        return;
                    }
                    else
                    {
                        context.MoveMessageToPoison(opMessage.ToJson());
                        await _slackNotifier.ErrorAsync($"Moved message {opMessage.OperationId} to poison: no corresponding coinEvent");
                    }
                }
                else
                {
                    //Process transfer/cashout operations
                    foreach (var match in historicalMessages)
                    {
                        if (!string.IsNullOrEmpty(match.TransactionHash) &&
                            await _ethereumTransactionService.IsTransactionExecuted(match.TransactionHash, Constants.GasForCoinTransaction))
                        {
                            var coinEvent = await _coinEventService.GetCoinEventById(match.OperationId);
                            if (coinEvent != null && coinEvent.TransactionHash.ToLower() == match.TransactionHash.ToLower())
                            {
                                await ResubmittTransactionAsync(match.TransactionHash, operatioId, opResubmitCounter);

                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (opMessage.DequeueCount > 100000)
                {
                    context.MoveMessageToPoison(opMessage.ToJson());
                    await _slackNotifier.ErrorAsync($"Moved message {opMessage.OperationId} to poison: dequeue count is {opMessage.DequeueCount }" +
                        $" error is {ex.Message}");

                    return;
                }
                opMessage.LastError = ex.Message;
                opMessage.DequeueCount++;
                context.MoveMessageToEnd(opMessage.ToJson());

                await _log.WriteErrorAsync("MonitoringOperationJob", "Execute", "", ex);

                return;
            }

            if (opMessage.DequeueCount > 100000)
            {
                context.MoveMessageToPoison(opMessage.ToJson());
                await _slackNotifier.ErrorAsync($"Moved message {opMessage.OperationId} to poison: dequeue count is {opMessage.DequeueCount }");
            }
            else
            {
                opMessage.DequeueCount++;
                context.MoveMessageToEnd(opMessage.ToJson());
            }
        }


        private async Task ResubmittTransactionAsync(string transactionHash, string operationId, IOperationResubmitt counter)
        {
            await _transactionMonitoringQueue.PutRawMessageAsync(
                            Newtonsoft.Json.JsonConvert.SerializeObject(
                                new CoinTransactionMessage()
                                {
                                    TransactionHash = transactionHash,
                                    OperationId = operationId,
                                    LastError = "FROM_JOB",
                                    PutDateTime = DateTime.UtcNow
                                }
                            ));

            counter.ResubmittCount++;
            await _operationResubmittRepository.InsertOrReplaceAsync(counter);
        }
    }
}


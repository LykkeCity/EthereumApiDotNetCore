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
using Services.New.Models;
using System.Numerics;
using Core.Exceptions;
using AzureStorage.Queue;
using Newtonsoft.Json;
using EdjCase.JsonRpc.Client;

namespace EthereumJobs.Job
{
    public class MonitoringOperationJob
    {
        private readonly ILog _log;
        private readonly IBaseSettings _settings;
        private readonly IPendingOperationService _pendingOperationService;
        private readonly IExchangeContractService _exchangeContractService;
        private readonly ICoinEventService _coinEventService;
        private readonly ITransferContractService _transferContractService;
        private readonly IEventTraceRepository _eventTraceRepository;
        private readonly IQueueExt _coinEventResubmittQueue;

        public MonitoringOperationJob(
            ILog log,
            IBaseSettings settings,
            IPendingOperationService pendingOperationService, 
            IExchangeContractService exchangeContractService,
            ICoinEventService coinEventService,
            ITransferContractService transferContractService, 
            IEventTraceRepository eventTraceRepository,
            IQueueFactory queueFactory)
        {
            _eventTraceRepository = eventTraceRepository;
            _exchangeContractService = exchangeContractService;
            _pendingOperationService = pendingOperationService;
            _settings = settings;
            _log = log;
            _coinEventService = coinEventService;
            _transferContractService = transferContractService;
            _coinEventResubmittQueue = queueFactory.Build(Constants.CoinEventResubmittQueue);
        }

        [QueueTrigger(Constants.PendingOperationsQueue, 100, true)]
        public async Task Execute(OperationHashMatchMessage opMessage, QueueTriggeringContext context)
        {
            await ProcessOperation(opMessage, context, _exchangeContractService.Transfer);
            
        }

        public async Task ProcessOperation(OperationHashMatchMessage opMessage, QueueTriggeringContext context, 
            Func<Guid, string, string, string, BigInteger, string, Task<string>> transferDelegate)
        {
            try
            {
                var operation = await _pendingOperationService.GetOperationAsync(opMessage.OperationId);
                if (operation == null)
                {
                    await _coinEventResubmittQueue.PutRawMessageAsync(JsonConvert.SerializeObject(opMessage));

                    return;
                }
                var guid = Guid.Parse(operation.OperationId);
                var amount = BigInteger.Parse(operation.Amount);

                BigInteger resultAmount;
                string transactionHash = null;
                CoinEventType? eventType = null;
                BigInteger currentBalance = await _transferContractService.GetBalanceOnAdapter(
                    operation.CoinAdapterAddress, 
                    operation.FromAddress,
                    checkInPendingBlock: true);

                switch (operation.OperationType)
                {
                    case OperationTypes.Cashout:
                        eventType = CoinEventType.CashoutStarted;
                        resultAmount = amount;
                        if (!CheckBalance(currentBalance, resultAmount)) break;
                        transactionHash = await _exchangeContractService.CashOut(guid,
                            operation.CoinAdapterAddress,
                            operation.FromAddress,
                            operation.ToAddress, amount, operation.SignFrom);
                        break;
                    case OperationTypes.Transfer:
                        eventType = CoinEventType.TransferStarted;
                        resultAmount = amount;
                        if (!CheckBalance(currentBalance, resultAmount)) break;
                        transactionHash = await transferDelegate(guid, operation.CoinAdapterAddress,
                            operation.FromAddress,
                            operation.ToAddress, amount, operation.SignFrom);
                        break;
                    case OperationTypes.TransferWithChange:
                        eventType = CoinEventType.TransferStarted;
                        BigInteger change = BigInteger.Parse(operation.Change);
                        resultAmount = amount - change;
                        if (!CheckBalance(currentBalance, resultAmount)) break;
                        transactionHash = await _exchangeContractService.TransferWithChange(guid, operation.CoinAdapterAddress,
                            operation.FromAddress,
                            operation.ToAddress, amount, operation.SignFrom, change, operation.SignTo);
                        break;
                    default:
                        await _log.WriteWarningAsync("MonitoringOperationJob", "Execute", $"Can't find right operation type for {opMessage.OperationId}", "");
                        break;
                }

                if (transactionHash != null && eventType != null)
                {
                    await _pendingOperationService.MatchHashToOpId(transactionHash, operation.OperationId);
                    await _coinEventService.PublishEvent(new CoinEvent(operation.OperationId.ToString(), transactionHash, operation.FromAddress, operation.ToAddress, resultAmount.ToString(), eventType.Value, operation.CoinAdapterAddress));
                    await _eventTraceRepository.InsertAsync(new EventTrace()
                    {
                        Note = $"Operation Processed. Put it in the {Constants.TransactionMonitoringQueue}. With hash {transactionHash}",
                        OperationId = operation.OperationId,
                        TraceDate = DateTime.UtcNow
                    });

                    return;
                }
            }
            catch (ClientSideException clientSideExc) when (clientSideExc.ExceptionType == ExceptionType.OperationWithIdAlreadyExists)
            {
                await _coinEventResubmittQueue.PutRawMessageAsync(JsonConvert.SerializeObject(opMessage));

                return;
            }
            catch (RpcClientException exc)
            {
                await _log.WriteErrorAsync("MonitoringOperationJob", "Execute", "RpcException", exc);
                opMessage.LastError = exc.Message;
                opMessage.DequeueCount++;
                if (opMessage.DequeueCount < 6)
                {
                    context.MoveMessageToEnd(opMessage.ToJson());
                    context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, 200);
                }
                else
                {
                    context.MoveMessageToPoison(opMessage.ToJson());
                }

                return;
            }
            catch (Exception ex)
            {
                if (ex.Message != opMessage.LastError)
                    await _log.WriteWarningAsync("MonitoringOperationJob", "Execute", $"OperationId: [{opMessage.OperationId}]", "");

                opMessage.LastError = ex.Message;
                opMessage.DequeueCount++;
                context.MoveMessageToPoison(opMessage.ToJson());

                await _log.WriteErrorAsync("MonitoringOperationJob", "Execute", "", ex);

                return;
            }

            opMessage.DequeueCount++;
            context.MoveMessageToEnd(opMessage.ToJson());
            context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, 200);
        }

        private bool CheckBalance(BigInteger currentBalance, BigInteger amount)
        {
            return currentBalance >= amount;
        }
    }
}

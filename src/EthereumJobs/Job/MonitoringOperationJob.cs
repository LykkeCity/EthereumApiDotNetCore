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

namespace EthereumJobs.Job
{
    public class MonitoringOperationJob
    {
        private readonly ILog _log;
        private readonly IBaseSettings _settings;
        private readonly IPendingOperationService _pendingOperationService;
        private readonly IExchangeContractService _exchangeContractService;

        public MonitoringOperationJob(ILog log, IBaseSettings settings,
            IPendingOperationService pendingOperationService, IExchangeContractService exchangeContractService)
        {
            _exchangeContractService = exchangeContractService;
            _pendingOperationService = pendingOperationService;
            _settings = settings;
            _log = log;
        }

        [QueueTrigger(Constants.PendingOperationsQueue, 100, true)]
        public async Task Execute(OperationHashMatchMessage opMessage, QueueTriggeringContext context)
        {
            try
            {
                var operation = await _pendingOperationService.GetOperationAsync(opMessage.OperationId);
                var guid = Guid.Parse(operation.OperationId);
                var amount = BigInteger.Parse(operation.Amount);
                string transactionHash = null;
                switch (operation.OperationType)
                {
                    case OperationTypes.Cashout:
                        transactionHash = await _exchangeContractService.CashOut(guid, 
                            operation.CoinAdapterAddress, 
                            operation.FromAddress,
                            operation.ToAddress, amount, operation.SignFrom);
                        break;
                    case OperationTypes.Transfer:
                        transactionHash = await _exchangeContractService.Transfer(guid, operation.CoinAdapterAddress,
                            operation.FromAddress,
                            operation.ToAddress, amount, operation.SignFrom);
                        break;
                    case OperationTypes.TransferWithChange:
                        BigInteger change = BigInteger.Parse(operation.Change);
                        transactionHash = await _exchangeContractService.TransferWithChange(guid, operation.CoinAdapterAddress,
                            operation.FromAddress,
                            operation.ToAddress, amount, operation.SignFrom, change, operation.SignTo);
                        break;
                    default:
                        await _log.WriteWarningAsync("MonitoringOperationJob", "Execute", $"Can't find right operation type for {opMessage.OperationId}", "");
                        break;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message != opMessage.LastError)
                    await _log.WriteWarningAsync("MonitoringOperationJob", "Execute", $"OperationId: [{opMessage.OperationId}]", "");

                opMessage.LastError = ex.Message;

                opMessage.DequeueCount++;
                context.MoveMessageToEnd(opMessage.ToJson());
                context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, 200);

                await _log.WriteErrorAsync("MonitoringOperationJob", "Execute", "", ex);
                return;
            }
        }
    }
}

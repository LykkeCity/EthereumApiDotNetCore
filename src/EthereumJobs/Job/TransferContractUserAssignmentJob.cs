using System.Threading.Tasks;
using Core.Repositories;
using Nethereum.Web3;
using Services;
using Common.Log;
using Core.Settings;
using System.Numerics;
using System;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;
using Core;
using Lykke.JobTriggers.Triggers.Bindings;

namespace EthereumJobs.Job
{
    public class TransferContractUserAssignmentJob { 
        private readonly ILog _logger;
        private readonly ITransferContractUserAssignmentQueueService _transferContractUserAssignmentQueueService;
        private readonly IBaseSettings _settings;
        private readonly ICoinRepository _coinRepository;

        public TransferContractUserAssignmentJob(IBaseSettings settings,
            ILog logger,
            ITransferContractUserAssignmentQueueService transferContractUserAssignmentQueueService,
            ICoinRepository coinRepository
            )
        {
            _settings = settings;
            _logger = logger;
            _transferContractUserAssignmentQueueService = transferContractUserAssignmentQueueService;
            _coinRepository = coinRepository;
        }

        [QueueTrigger(Constants.TransferContractUserAssignmentQueueName, 100, true)]
        public async Task Execute(TransferContractUserAssignment transaction, QueueTriggeringContext context)
        {
            try
            {
                await _transferContractUserAssignmentQueueService.CompleteTransfer(transaction);
            }
            catch (Exception ex)
            {
                if (ex.Message != transaction.LastError)
                    await _logger.WriteWarningAsync("MonitoringCoinTransactionJob", "Execute", $"TransferContractAddress: [{transaction.TransferContractAddress}]", "");

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
                await _logger.WriteErrorAsync("MonitoringCoinTransactionJob", "Execute", "", ex);
            }
        }
    }
}

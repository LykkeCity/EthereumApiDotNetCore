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
        private readonly ITransferContractService _transferContractService;

        public TransferContractUserAssignmentJob(IBaseSettings settings,
            ILog logger,
            ITransferContractUserAssignmentQueueService transferContractUserAssignmentQueueService,
            ICoinRepository coinRepository,
            ITransferContractService transferContractService
            )
        {
            _transferContractService = transferContractService;
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
                string assignedUser = await _transferContractService.GetTransferAddressUser(transaction.CoinAdapterAddress, transaction.TransferContractAddress);

                if (string.IsNullOrEmpty(assignedUser) || assignedUser == "0x0000000000000000000000000000000000000000")
                {
                    await _transferContractUserAssignmentQueueService.CompleteTransfer(transaction);
                }
                else
                {
                    await _logger.WriteInfoAsync("TransferContractUserAssignmentJob", "Execute", $"{transaction.TransferContractAddress}", 
                        $"Skipp assignment, current user {assignedUser}",DateTime.UtcNow);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message != transaction.LastError)
                    await _logger.WriteWarningAsync("TransferContractUserAssignmentJob", "Execute", $"TransferContractAddress: [{transaction.TransferContractAddress}]", "");

                transaction.LastError = ex.Message;

                if (transaction.DequeueCount >= 4)
                {
                    context.MoveMessageToPoison();
                }
                else
                {
                    transaction.DequeueCount++;
                    context.MoveMessageToEnd();
                    context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, 200);
                }
                await _logger.WriteErrorAsync("TransferContractUserAssignmentJob", "Execute", "", ex);
            }
        }
    }
}

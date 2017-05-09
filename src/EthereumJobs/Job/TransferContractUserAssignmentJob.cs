using System.Threading.Tasks;
using Core.Repositories;
using Nethereum.Web3;
using Services;
using Common.Log;
using Core.Settings;
using System.Numerics;
using System;
using Common;

namespace EthereumJobs.Job
{
    public class TransferContractUserAssignmentJob : TimerPeriod
    {
        //10 minutes
        private const int TimerPeriodSeconds = 60;
        private const int AlertNotChangedBalanceCount = 3;

        private readonly ILog _logger;
        private readonly ITransferContractUserAssignmentQueueService _transferContractUserAssignmentQueueService;
        private readonly IBaseSettings _settings;
        private readonly ICoinRepository _coinRepository;

        public TransferContractUserAssignmentJob(IBaseSettings settings,
            ILog logger,
            ITransferContractUserAssignmentQueueService transferContractUserAssignmentQueueService,
            ICoinRepository coinRepository
            ) :
            base("MonitoringTransferContracts", TimerPeriodSeconds * 1000, logger)
        {
            _settings = settings;
            _logger = logger;
            _transferContractUserAssignmentQueueService = transferContractUserAssignmentQueueService;
            _coinRepository = coinRepository;
        }

        public override async Task Execute()
        {
            try
            {
                while (await _transferContractUserAssignmentQueueService.CompleteTransfer() && Working)
                {
                }
            }
            catch (Exception ex)
            {
                await _logger.WriteErrorAsync("EthereumJob", "TransferContractUserAssignmentJob", "", ex);
            }
        }
    }
}

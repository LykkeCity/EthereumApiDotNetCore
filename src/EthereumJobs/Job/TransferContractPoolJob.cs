using System.Threading.Tasks;
using Core.Repositories;
using Core.Timers;
using Nethereum.Web3;
using Services;
using Common.Log;
using Core.Settings;
using System.Numerics;
using System;
using System.Collections.Generic;

namespace EthereumJobs.Job
{
    public class TransferContractPoolJob : TimerPeriod
    {
        //10 minutes
        private const int TimerPeriodSeconds = 60 * 10;
        private const int AlertNotChangedBalanceCount = 3;

        private readonly ILog _logger;
        private readonly ICoinRepository _coinRepository;
        private readonly TransferContractPoolService _transferContractPoolService;

        public TransferContractPoolJob(IBaseSettings settings,
            ILog logger,
            ICoinRepository coinRepository,
            TransferContractPoolService transferContractPoolService
            ) :
            base("MonitoringTransferContracts", TimerPeriodSeconds * 1000, logger)
        {
            _logger = logger;
            _coinRepository = coinRepository;
            _transferContractPoolService = transferContractPoolService;
        }

        public override async Task Execute()
        {
            await _coinRepository.ProcessAllAsync(async (items) =>
            {
                foreach (var item in items)
                {
                    await _transferContractPoolService.Execute(item);
                }
            });
        }
    }
}

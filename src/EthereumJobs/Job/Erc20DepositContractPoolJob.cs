﻿using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.JobTriggers.Triggers.Attributes;
using Services;

namespace EthereumJobs.Job
{
    public class Erc20DepositContractPoolJob
    {
        private readonly IErc20DepositContractPoolService _contractPoolService;
        private readonly ILog _logger;

        public Erc20DepositContractPoolJob(
            IErc20DepositContractPoolService contractPoolService,
            ILog logger)
        {
            _contractPoolService = contractPoolService;
            _logger = logger;
        }

        [TimerTrigger("0.00:01:00")]
        public async Task Execute()
        {
            try
            {
                await _contractPoolService.ReplenishPool();
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync(nameof(Erc20DepositContractPoolJob), nameof(Execute), "", e, DateTime.UtcNow);
            }
        }
    }
}
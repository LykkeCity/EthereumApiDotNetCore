﻿using System;
using System.Threading.Tasks;
using Common.Log;
using Core;
using Lykke.JobTriggers.Triggers.Attributes;
using Services;

namespace EthereumJobs.Job
{
    public class Erc20DepositContractPoolRenewJob
    {
        private readonly ILog _logger;
        private readonly IErc20DepositContractQueueServiceFactory _poolFactory;

        public Erc20DepositContractPoolRenewJob(
            ILog logger,
            IErc20DepositContractQueueServiceFactory poolFactory)
        {
            _logger = logger;
            _poolFactory = poolFactory;
        }


        [TimerTrigger("1.00:00:00")]
        public async Task Execute()
        {
            await _logger.WriteInfoAsync(nameof(Erc20DepositContractPoolRenewJob), nameof(Execute), "", "Job has been started ", DateTime.UtcNow);

            var pool = _poolFactory.Get(Constants.Erc20DepositContractPoolQueue);
            var count = await pool.Count();

            for (var i = 0; i < count; i++)
            {
                var contract = await pool.GetContractAddress();

                if (contract == null)
                {
                    break;
                }
                    
                await pool.PushContractAddress(contract);
            }
        }
    }
}
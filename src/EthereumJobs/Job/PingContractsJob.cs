﻿
using System.Threading.Tasks;
using Services.Coins;
using Common.Log;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;
using Core.Repositories;
using Services;
using System;
using Core.Notifiers;

namespace EthereumJobs.Job
{
    public class PingContractsJob
    {
        private readonly IExchangeContractService _exchangeContractService;
        private readonly ILog _log;
        private readonly ISlackNotifier _slackNotifier;

        public PingContractsJob(IExchangeContractService exchangeContractService, ILog log, ISlackNotifier slackNotifier)
        {
            _log = log;
            _exchangeContractService = exchangeContractService;
            _slackNotifier = slackNotifier;
        }

        [TimerTrigger("7.00:00:00")]
        public async Task Execute()
        {
            try
            {
                string hash = await _exchangeContractService.PingMainExchangeContract();
                await _log.WriteInfoAsync("PingContractsJob", "Execute", "", $"MainExchange has been pinged trHash {hash}", DateTime.UtcNow);
                await _slackNotifier.WarningAsync($"Main Exchange conrtract was pinged {DateTime.UtcNow} - utc, {hash} - trHash");
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync("PingContractsJob", "Execute", "", e, DateTime.UtcNow);
            }
        }
    }
}

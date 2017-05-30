
using System.Threading.Tasks;
using Services.Coins;
using Common.Log;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;
using Core.Repositories;
using Services;
using System;

namespace EthereumJobs.Job
{
    public class PingContractsJob
    {
        private readonly IExchangeContractService _exchangeContractService;
        private readonly ILog _log;

        public PingContractsJob(IExchangeContractService exchangeContractService, ILog log)
        {
            _log = log;
            _exchangeContractService = exchangeContractService;
        }

        [TimerTrigger("1.00:00:00")]
        public async Task Execute()
        {
            try
            {
                await _exchangeContractService.PingMainExchangeContract();
                await _log.WriteInfoAsync("PingContractsJob", "Execute", "", "MainExchange has been pinged", DateTime.UtcNow);
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync("PingContractsJob", "Execute", "", e, DateTime.UtcNow);
            }
        }
    }
}

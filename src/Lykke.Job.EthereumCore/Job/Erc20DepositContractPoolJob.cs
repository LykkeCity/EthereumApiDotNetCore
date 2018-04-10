using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Common.Log;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Services;

namespace Lykke.Job.EthereumCore.Job
{
    public class Erc20DepositContractPoolJob
    {
        private readonly IErc20DepositContractPoolService _contractPoolService;
        private readonly IErc20DepositContractPoolService _contractPoolServiceLykkePay;

        private readonly ILog _logger;

        public Erc20DepositContractPoolJob(
            [KeyFilter(Constants.DefaultKey)]IErc20DepositContractPoolService contractPoolService,
            [KeyFilter(Constants.LykkePayKey)]IErc20DepositContractPoolService contractPoolServiceLykkePay,
            ILog logger)
        {
            _contractPoolService = contractPoolService;
            _contractPoolServiceLykkePay = contractPoolServiceLykkePay;
            _logger = logger;
        }

        [TimerTrigger("0.00:01:00")]
        public async Task Execute()
        {
            try
            {
                await _contractPoolService.ReplenishPool();
                await _logger.WriteInfoAsync(nameof(Erc20DepositContractPoolJob), nameof(Execute), "", "Pool have been replenished", DateTime.UtcNow);
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync(nameof(Erc20DepositContractPoolJob), nameof(Execute), "", e, DateTime.UtcNow);
            }
        }

        [TimerTrigger("0.00:01:00")]
        public async Task ExecuteForLykkeApi()
        {
            try
            {
                await _contractPoolServiceLykkePay.ReplenishPool();
                await _logger.WriteInfoAsync(nameof(Erc20DepositContractPoolJob), nameof(ExecuteForLykkeApi), "", "Pool have been replenished", DateTime.UtcNow);
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync(nameof(Erc20DepositContractPoolJob), nameof(ExecuteForLykkeApi), "", e, DateTime.UtcNow);
            }
        }
    }
}
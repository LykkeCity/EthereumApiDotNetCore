using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Nethereum.Web3;
using Lykke.Service.EthereumCore.Services;
using Common.Log;
using Lykke.Service.EthereumCore.Core.Settings;
using System.Numerics;
using System;
using System.Collections.Generic;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;

namespace Lykke.Job.EthereumCore.Job
{
    public class TransferContractPoolJob
    {
        private readonly ILog _logger;
        private readonly ICoinRepository _coinRepository;
        private readonly TransferContractPoolService _transferContractPoolService;

        public TransferContractPoolJob(IBaseSettings settings,
            ILog logger,
            ICoinRepository coinRepository,
            TransferContractPoolService transferContractPoolService
            )
        {
            _logger = logger;
            _coinRepository = coinRepository;
            _transferContractPoolService = transferContractPoolService;
        }

        [TimerTrigger("0.00:01:00")]
        public async Task Execute()
        {
            await _coinRepository.ProcessAllAsync(async (items) =>
            {
                foreach (var item in items)
                {
                    try
                    {
                        await _transferContractPoolService.Execute(item);
                    }
                    catch (Exception e)
                    {
                        await _logger.WriteErrorAsync("TransferContractPoolJob", "Execute", "", e, DateTime.UtcNow);
                    }
                }
            });
        }
    }
}

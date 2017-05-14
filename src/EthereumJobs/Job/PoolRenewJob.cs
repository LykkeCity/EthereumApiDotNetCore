using Common.Log;
using Core;
using Core.Repositories;
using Core.Settings;
using Lykke.JobTriggers.Triggers.Attributes;
using Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EthereumJobs.Job
{
    public class PoolRenewJob
    {
        private readonly ILog _logger;
        private readonly IBaseSettings _baseSettings;
        private readonly ICoinRepository _coinRepository;
        private readonly ITransferContractQueueServiceFactory _transferContractQueueServiceFactory;

        public PoolRenewJob(ILog logger, ICoinRepository coinRepository, IBaseSettings baseSettings,
            ITransferContractQueueServiceFactory transferContractQueueServiceFactory)
        {
            _logger = logger;
            _coinRepository = coinRepository;
            _baseSettings = baseSettings;
            _transferContractQueueServiceFactory = transferContractQueueServiceFactory;
        }

        [TimerTrigger("1.00:00:00")]
        public async Task RenewPool()
        {
            await RenewTransferContracts();
        }

        private async Task RenewTransferContracts()
        {
            await _coinRepository.ProcessAllAsync(async (coins) =>
            {
                foreach (var coin in coins)
                {
                    try
                    {
                        string coinPoolQueueName = QueueHelper.GenerateQueueNameForContractPool(coin.AdapterAddress);
                        ITransferContractQueueService transferContractQueueService =
                            _transferContractQueueServiceFactory.Get(coinPoolQueueName);
                        var count = await transferContractQueueService.Count();

                        for (int i = 0; i < count; i++)
                        {
                            var cobtract = await transferContractQueueService.GetContract();
                            if (cobtract == null)
                                return;
                            await transferContractQueueService.PushContract(cobtract);
                        }
                    }
                    catch (Exception e)
                    {
                        await _logger.WriteErrorAsync("PoolRenewJob", "RenewTransferContracts", "", e);
                    }
                }
            });
        }
    }
}

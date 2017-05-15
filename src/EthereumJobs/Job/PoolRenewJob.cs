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
        public async Task Execute()
        {
            await _logger.WriteInfoAsync("PoolRenewJob", "Execute", "", "PoolRenewJob has been started ", DateTime.UtcNow);
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
                            var contract = await transferContractQueueService.GetContract();
                            if (contract == null)
                                return;
                            await transferContractQueueService.PushContract(contract);
                        }

                        await _logger.WriteInfoAsync("PoolRenewJob", "Execute", "", $"PoolRenewJob has been finished for {count} contracts in {coinPoolQueueName} ", DateTime.UtcNow);
                    }
                    catch (Exception e)
                    {
                        await _logger.WriteErrorAsync("PoolRenewJob", "Execute", "", e);
                    }
                }
            });
        }
    }
}

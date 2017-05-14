
using System.Threading.Tasks;
using Services.Coins;
using Common.Log;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;
using Core.Repositories;
using Services;

namespace EthereumJobs.Job
{
    public class PingContractsJob
    {
        private readonly IExchangeContractService _exchangeContractService;
        private readonly ICoinRepository _coinRepository;
        private readonly AssetContractService _assetContractService;

        public PingContractsJob(IExchangeContractService exchangeContractService, ILog log, ICoinRepository coinRepository, AssetContractService assetContractService)
        {
            _exchangeContractService = exchangeContractService;
            _coinRepository = coinRepository;
            _assetContractService = assetContractService;
        }

        [TimerTrigger("1.00:00:00")]
        public async Task Execute()
        {
            await _exchangeContractService.PingMainExchangeContract();

            await _coinRepository.ProcessAllAsync(async (coins) =>
            {
                foreach (var coin in coins)
                {
                    await _assetContractService.PingAdapterContract(coin.AdapterAddress);
                }
            });
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Airlines;
using Lykke.Service.EthereumCore.Core.Settings;

namespace Lykke.Service.EthereumCore.Services.Airlines
{
    public class Erc20DepositContractPoolService : IErc20DepositContractPoolService
    {
        private readonly IAirlinesErc20DepositContractService _contractService;
        private readonly IErc20DepositContractQueueServiceFactory _poolFactory;
        private readonly AirlinesSettings _settings;

        public Erc20DepositContractPoolService(
            IAirlinesErc20DepositContractService contractService,
            IErc20DepositContractQueueServiceFactory poolFactory,
            AirlinesSettings settings)
        {
            _contractService = contractService;
            _poolFactory = poolFactory;
            _settings = settings;
        }

        public async Task ReplenishPool()
        {
            var pool = _poolFactory.Get(Constants.LykkePayErc20DepositContractPoolQueue);
            var currentCount = await pool.Count();

            if (currentCount < _settings.MinContractPoolLength)
            {
                while (currentCount < _settings.MaxContractPoolLength)
                {
                    var tasks = Enumerable.Range(0, _settings.ContractsPerRequest).Select(x => _contractService.CreateContract());
                    var trHashes = (await Task.WhenAll(tasks)).Where(x => !string.IsNullOrEmpty(x));
                    var contractAddresses = await _contractService.GetContractAddresses(trHashes);

                    await Task.WhenAll(contractAddresses.Select(pool.PushContractAddress));
                    
                    currentCount += _settings.ContractsPerRequest;
                }
            }
        }
    }
}
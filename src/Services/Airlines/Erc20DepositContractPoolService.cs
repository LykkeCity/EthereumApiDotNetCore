using System.Linq;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Airlines;
using Lykke.Service.EthereumCore.Core.Settings;

namespace Lykke.Service.EthereumCore.Services.Airlines
{
    public class AirlinesErc20DepositContractPoolService : IErc20DepositContractPoolService
    {
        private readonly IAirlinesErc20DepositContractService _contractService;
        private readonly IErc20DepositContractQueueServiceFactory _poolFactory;
        private readonly AppSettings _settings;

        public AirlinesErc20DepositContractPoolService(
            [KeyFilter(Constants.AirLinesKey)]IAirlinesErc20DepositContractService contractService,
            IErc20DepositContractQueueServiceFactory poolFactory,
            AppSettings settings)
        {
            _contractService = contractService;
            _poolFactory = poolFactory;
            _settings = settings;
        }

        public async Task ReplenishPool()
        {
            var pool = _poolFactory.Get(Constants.AirlinesErc20DepositContractPoolQueue);
            var currentCount = await pool.Count();

            if (currentCount < _settings.EthereumCore.MinContractPoolLength)
            {
                while (currentCount < _settings.EthereumCore.MaxContractPoolLength)
                {
                    var tasks = Enumerable.Range(0, _settings.EthereumCore.ContractsPerRequest)
                        .Select(x => _contractService.CreateContract());
                    var trHashes = (await Task.WhenAll(tasks)).Where(x => !string.IsNullOrEmpty(x));
                    var contractAddresses = await _contractService.GetContractAddresses(trHashes);

                    await Task.WhenAll(contractAddresses.Select(pool.PushContractAddress));
                    
                    currentCount += _settings.EthereumCore.ContractsPerRequest;
                }
            }
        }
    }
}
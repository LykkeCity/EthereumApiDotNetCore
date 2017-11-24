using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Settings;

namespace Services
{
    public class Erc20DepositContractPoolService : IErc20DepositContractPoolService
    {
        private readonly IErc20DepositContractService _contractService;
        private readonly IErc20DepositContractQueueServiceFactory _poolFactory;
        private readonly IBaseSettings _settings;

        public Erc20DepositContractPoolService(
            IErc20DepositContractService contractService,
            IErc20DepositContractQueueServiceFactory poolFactory,
            IBaseSettings settings)
        {
            _contractService = contractService;
            _poolFactory = poolFactory;
            _settings = settings;
        }

        public async Task ReplenishPool()
        {
            var pool = _poolFactory.Get(Constants.Erc20DepositContractPoolQueue);
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

    public interface IErc20DepositContractPoolService
    {
        Task ReplenishPool();
    }
}
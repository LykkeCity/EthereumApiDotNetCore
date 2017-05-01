using Core.Repositories;
using Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class AssetContractService
    {
        private readonly ICoinRepository _coinRepository;
        private readonly IContractService _contractService;
        private readonly IEthereumContractRepository _ethereumContractRepository;
        private readonly IBaseSettings _settings;

        public AssetContractService(IBaseSettings settings,
            IContractService contractService, 
            ICoinRepository coinRepository,
            IEthereumContractRepository ethereumContractRepository)
        {
            _settings = settings;
            _contractService = contractService;
            _coinRepository = coinRepository;
            _ethereumContractRepository = ethereumContractRepository;
        }

        public async Task<string> CreateCoinContract(ICoin coin, INewEthereumContract coinContract)
        {
            string coinAdapterAddress = 
                await _contractService.CreateContract(coinContract.Abi, 
                coinContract.ByteCode, _settings.MainExchangeContract.Address);
            coin.AdapterAddress = coinAdapterAddress;
            await _coinRepository.InsertOrReplace(coin);

            await _ethereumContractRepository.SaveAsync(new Core.Repositories.EthereumContract()
            {
                Abi = coinContract.Abi,
                ByteCode = coinContract.ByteCode,
                ContractAddress = coinAdapterAddress
            });

            return coinAdapterAddress;
        }
    }
}

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
        private readonly BaseSettings _settings;

        public AssetContractService(BaseSettings settings,IContractService contractService, 
            ITransferContractRepository transferContractRepository, ICoinRepository coinRepository)
        {
            _settings = settings;
            _contractService = contractService;
            _coinRepository = coinRepository;
        }

        public async Task CreateCoinContract(ICoin coin, INewEthereumContract coinContract)
        {
            string coinAdapterAddress = 
                await _contractService.CreateContract(coinContract.Abi, 
                coinContract.ByteCode, _settings.MainExchangeContract.Address);
            coin.AdapterAddress = coinAdapterAddress;
            await _coinRepository.InsertOrReplace(coin);
        }
    }
}

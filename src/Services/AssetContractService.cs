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
        private readonly IErcInterfaceService _ercInterfaceService;
        private readonly IBaseSettings _settings;

        public AssetContractService(IBaseSettings settings,
            IContractService contractService,
            ICoinRepository coinRepository,
            IEthereumContractRepository ethereumContractRepository,
            IErcInterfaceService ercInterfaceService)
        {
            _settings = settings;
            _contractService = contractService;
            _coinRepository = coinRepository;
            _ercInterfaceService = ercInterfaceService;
        }

        public async Task<string> CreateCoinContract(ICoin coin)
        {
            if (coin == null)
            {
                throw new ArgumentException("Coin should not be null", "coin");
            }

            string abi;
            string byteCode;
            string[] constructorParametes;

            if (coin.ContainsEth)
            {
                abi = _settings.EthAdapterContract.Abi;
                byteCode = _settings.EthAdapterContract.ByteCode;
                constructorParametes = new string[] { _settings.MainExchangeContract.Address };
            }
            else
            {
                if (string.IsNullOrEmpty(coin.ExternalTokenAddress))
                {
                    throw new Exception("coin.ExternalTokenAddress should not be empty");
                }

                //TODO: check that external exists
                abi = _settings.TokenAdapterContract.Abi;
                byteCode = _settings.TokenAdapterContract.ByteCode;
                constructorParametes = new string[] { _settings.MainExchangeContract.Address, coin.ExternalTokenAddress };
            }

            string coinAdapterAddress =
                await _contractService.CreateContract(abi,
                byteCode, constructorParametes);
            coin.AdapterAddress = coinAdapterAddress;
            await _coinRepository.InsertOrReplace(coin);

            return coinAdapterAddress;
        }
    }
}

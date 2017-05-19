using Core.Exceptions;
using Core.Repositories;
using Core.Settings;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Services
{
    public class AssetContractService
    {
        private readonly ICoinRepository _coinRepository;
        private readonly IContractService _contractService;
        private readonly IErcInterfaceService _ercInterfaceService;
        private readonly IBaseSettings _settings;
        private readonly Web3 _web3;
        private readonly ITransferContractService _transferContractService;

        public AssetContractService(IBaseSettings settings,
            IContractService contractService,
            ICoinRepository coinRepository,
            IEthereumContractRepository ethereumContractRepository,
            IErcInterfaceService ercInterfaceService, 
            Web3 web3, 
            ITransferContractService transferContractService)
        {
            _transferContractService = transferContractService;
            _settings = settings;
            _contractService = contractService;
            _coinRepository = coinRepository;
            _ercInterfaceService = ercInterfaceService;
            _web3 = web3;
        }

        public Task<IEnumerable<ICoin>> GetAll()
        {
            return _coinRepository.GetAll();
        }

        public async Task<string> CreateCoinContract(ICoin coin)
        {
            if (coin == null)
            {
                throw new ClientSideException(ExceptionType.MissingRequiredParams,"Coin should not be null");
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
                    throw new ClientSideException(ExceptionType.MissingRequiredParams, "coin.ExternalTokenAddress should not be empty");
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

        public async Task<ICoin> GetById(string id)
        {
            var coin = await _coinRepository.GetCoin(id);

            return coin;
        }

        public async Task<ICoin> GetByAddress(string adapterAddress)
        {
            var coin = await _coinRepository.GetCoinByAddress(adapterAddress);

            return coin;
        }

        public async Task<string> PingAdapterContract(string adapterAddress)
        {
            string abi = _settings.CoinAbi;

            var contract = _web3.Eth.GetContract(abi, adapterAddress);
            var function = contract.GetFunction("ping");
            
            string transactionHash =
                await function.SendTransactionAsync(_settings.EthereumMainAccount);

            return transactionHash;
        }

        public async Task<BigInteger> GetBalance(string coinAdapterAddress, string userAddress)
        {
            BigInteger balance = await _transferContractService.GetBalanceOnAdapter(coinAdapterAddress, userAddress);

            return balance;
        }
    }
}

using Core;
using Core.Repositories;
using Core.Settings;
using Core.Utils;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Services
{
    public class TransferContractService
    {
        private readonly ICoinRepository _coinRepository;
        private readonly IContractService _contractService;
        private readonly BaseSettings _settings;
        private readonly ITransferContractRepository _transferContractRepository;

        public TransferContractService(IContractService contractService,
            ITransferContractRepository transferContractRepository, ICoinRepository coinRepository, BaseSettings settings)
        {
            _coinRepository = coinRepository;
            _contractService = contractService;
            _transferContractRepository = transferContractRepository;
            _settings = settings;
        }

        public async Task CreateTransferContract(string userAddress, string coinAdapterAddress,
            string externalTokenAddress, bool containsEth)
        {
            ICoin coin = await _coinRepository.GetCoinByAddress(coinAdapterAddress);

            if (coin == null)
            {
                throw new Exception($"Coin with address {coinAdapterAddress} does not exist");
            }

            string abi;
            string byteCode;

            //TODO: Adjust configuration for transfer contracts
            if (containsEth)
            {
                abi = _settings.EthTransferContract.Abi;
                byteCode = _settings.EthTransferContract.ByteCode;
            }
            else
            {
                abi = _settings.EthTransferContract.Abi;
                byteCode = _settings.EthTransferContract.ByteCode;
            }

            string transferContractAddress = await _contractService.CreateContract(abi, byteCode,
                userAddress, coinAdapterAddress);

            await _transferContractRepository.SaveAsync(new TransferContract()
            {
                CoinAdapterAddress = coinAdapterAddress,
                ContainsEth = containsEth,
                ContractAddress = transferContractAddress,
                ExternalTokenAddress = externalTokenAddress,
                UserAddress = userAddress,
            });
        }

        public async Task<string> RecievePaymentFromTransferContract(Guid id, string transferContractAddress,
            string coinAdapterAddress, BigInteger amount, bool containsEth)
        {
            var web3 = new Web3(_settings.EthereumUrl);

            await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount,
                _settings.EthereumMainAccountPassword, new HexBigInteger(120));

            ICoin coinDb = await _coinRepository.GetCoin(coinAdapterAddress);

            if (!coinDb.BlockchainDepositEnabled)
                throw new Exception("Coin must be payable");

            Contract contract;

            if (containsEth)
            {
                contract = web3.Eth.GetContract(_settings.EthTransferContract.Abi, transferContractAddress);
            }
            else
            {
                contract = web3.Eth.GetContract(_settings.TokenTransferContract.Abi, transferContractAddress);
            }

            var cashin = contract.GetFunction("cashin");
            var blockchainAmount = amount;
            var convertedId = EthUtils.GuidToBigInteger(id);
            string tr;

            if (!containsEth)
            {
                tr = await cashin.SendTransactionAsync(_settings.EthereumMainAccount,
                new HexBigInteger(Constants.GasForCoinTransaction),
                        new HexBigInteger(0), convertedId, coinDb.AdapterAddress,
                        coinAdapterAddress, blockchainAmount, Constants.GasForCoinTransaction, new byte[0]);
            }
            else
            {
                tr = await cashin.SendTransactionAsync(_settings.EthereumMainAccount,
               new HexBigInteger(Constants.GasForCoinTransaction),
                       new HexBigInteger(blockchainAmount), convertedId, coinDb.AdapterAddress,
                       coinAdapterAddress, 0, Constants.GasForCoinTransaction, new byte[0]);
            }

            return tr;
        }
    }
}

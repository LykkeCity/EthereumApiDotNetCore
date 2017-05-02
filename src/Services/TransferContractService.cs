﻿using Core;
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
        private readonly IBaseSettings _settings;
        private readonly ITransferContractRepository _transferContractRepository;

        public TransferContractService(IContractService contractService,
            ITransferContractRepository transferContractRepository, ICoinRepository coinRepository, IBaseSettings settings)
        {
            _coinRepository = coinRepository;
            _contractService = contractService;
            _transferContractRepository = transferContractRepository;
            _settings = settings;
        }

        public async Task<string> CreateTransferContract(string userAddress, string coinAdapterAddress,
            string externalTokenAddress, bool containsEth)
        {
            ITransferContract contract = await GetTransferContract(userAddress, coinAdapterAddress);

            if (contract != null)
            {
                throw new Exception($"Transfer account for {userAddress} - {coinAdapterAddress} already exists");
            }

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
                abi = _settings.TokenTransferContract.Abi;
                byteCode = _settings.TokenTransferContract.ByteCode;
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

            return transferContractAddress;
        }

        public async Task<ITransferContract> GetTransferContract(string userAddress, string coinAdapterAddress)
        {
            ITransferContract contract = await _transferContractRepository.GetAsync(userAddress, coinAdapterAddress);

            return contract;
        }

        public async Task<string> RecievePaymentFromTransferContract(Guid id, string transferContractAddress,
            string coinAdapterAddress, string userAddress, BigInteger amount, bool containsEth)
        {
            var web3 = new Web3(_settings.EthereumUrl);

            await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount,
                _settings.EthereumMainAccountPassword, 120);

            ICoin coinDb = await _coinRepository.GetCoinByAddress(coinAdapterAddress);

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

            //function cashin(uint id, address coin, address receiver, uint amount, uint gas, bytes params)
            if (!containsEth)
            {
                tr = await cashin.SendTransactionAsync(_settings.EthereumMainAccount,
                new HexBigInteger(Constants.GasForCoinTransaction));
            }
            else
            {
                tr = await cashin.SendTransactionAsync(_settings.EthereumMainAccount,
               new HexBigInteger(Constants.GasForEthCashin), new HexBigInteger(0));
            }

            return tr;
        }
    }
}

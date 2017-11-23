﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.Repositories;
using Core.Settings;

namespace Services
{
    public class Erc20DepositContractService : IErc20DepositContractService
    {
        private readonly IErc20DepositContractRepository _contractRepository;
        private readonly IContractService _contractService;
        private readonly IErc20DepositContractQueueServiceFactory _poolFactory;
        private readonly IBaseSettings _settings;


        public Erc20DepositContractService(
            IErc20DepositContractRepository contractRepository,
            IContractService contractService,
            IErc20DepositContractQueueServiceFactory poolFactory,
            IBaseSettings settings)
        {
            _contractRepository = contractRepository;
            _contractService = contractService;
            _poolFactory = poolFactory;
            _settings = settings;
        }


        public async Task<string> AssignContract(string userAddress)
        {
            var pool = _poolFactory.Get(Constants.Erc20DepositContractPoolQueue);
            var contractAddress = await pool.GetContractAddress();

            await _contractRepository.AddOrReplace(contractAddress, userAddress);

            return contractAddress;
        }

        public async Task<string> CreateContract()
        {
            var abi = _settings.Erc20DepositContract.Abi;
            var byteCode = _settings.Erc20DepositContract.ByteCode;
            
            return await _contractService.CreateContractWithoutBlockchainAcceptance(abi, byteCode);
        }

        public async Task<IEnumerable<string>> GetContractAddresses(IEnumerable<string> txHashes)
        {
            return await _contractService.GetContractsAddresses(txHashes);
        }

        public async Task<string> GetContractAddress(string userAddress)
        {
            return await _contractRepository.Get(userAddress);
        }
    }

    public interface IErc20DepositContractService
    {
        Task<string> AssignContract(string userAddress);

        Task<string> CreateContract();

        Task<IEnumerable<string>> GetContractAddresses(IEnumerable<string> txHashes);

        Task<string> GetContractAddress(string userAddress);
    }
}
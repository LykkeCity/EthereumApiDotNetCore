using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using Core;
using Core.Repositories;
using Core.Settings;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;

namespace Services
{
    public class Erc20DepositContractService : IErc20DepositContractService
    {
        /*
         function transferAllTokens(address _tokenAddress, address _to) onlyOwner public returns (bool success) {
        
        ERC20Interface erc20Contract = ERC20Interface(_tokenAddress);
        uint balance = erc20Contract.balanceOf(this); 

        if (balance <= 0 || _to == address(this)) {
            return false;
        }

        return erc20Contract.transferFrom(this, _to, balance);
    }
             */

        private readonly IErc20DepositContractRepository _contractRepository;
        private readonly IContractService _contractService;
        private readonly IErc20DepositContractQueueServiceFactory _poolFactory;
        private readonly IBaseSettings _settings;
        private readonly ILog _log;
        private readonly IWeb3 _web3;

        public Erc20DepositContractService(
            IErc20DepositContractRepository contractRepository,
            IContractService contractService,
            IErc20DepositContractQueueServiceFactory poolFactory,
            IBaseSettings settings,
            ILog log,
            IWeb3 web3)
        {
            _contractRepository = contractRepository;
            _contractService = contractService;
            _poolFactory = poolFactory;
            _settings = settings;
            _log = log;
            _web3 = web3;
        }


        public async Task<string> AssignContract(string userAddress)
        {
            var contractAddress = await GetContractAddress(userAddress);

            if (string.IsNullOrEmpty(contractAddress))
            {
                var pool = _poolFactory.Get(Constants.Erc20DepositContractPoolQueue);

                contractAddress = await pool.GetContractAddress();

                await _contractRepository.AddOrReplace(new Erc20DepositContract
                {
                    ContractAddress = contractAddress,
                    UserAddress     = userAddress
                });
            }
            
            return contractAddress;
        }

        public async Task<string> CreateContract()
        {
            try
            {
                var abi = _settings.Erc20DepositContract.Abi;
                var byteCode = _settings.Erc20DepositContract.ByteCode;

                return await _contractService.CreateContractWithoutBlockchainAcceptance(abi, byteCode);
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(Erc20DepositContractService), nameof(CreateContract), "", e, DateTime.UtcNow);

                return null;
            }

        }

        public async Task<IEnumerable<string>> GetContractAddresses(IEnumerable<string> txHashes)
        {
            return await _contractService.GetContractsAddresses(txHashes);
        }

        public async Task<string> GetContractAddress(string userAddress)
        {
            return (await _contractRepository.Get(userAddress))?.ContractAddress;
        }

        public async Task ProcessAllAsync(Func<IErc20DepositContract, Task> processAction)
        {
            await _contractRepository.ProcessAllAsync(processAction);
        }

        /// <param name="depositContractAddress"></param>
        /// <param name="erc20TokenAddress"></param>
        /// <param name="destinationAddress"></param>
        /// <returns>TransactionHash</returns>
        public async Task<string> RecievePaymentFromDepositContract(string depositContractAddress,
           string erc20TokenAddress, string destinationAddress)
        {
            Contract contract = _web3.Eth.GetContract(_settings.Erc20DepositContract.Abi, depositContractAddress);
            var cashin = contract.GetFunction("transferAllTokens");
            string trHash = await cashin.SendTransactionAsync(_settings.EthereumMainAccount,
            new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0), erc20TokenAddress, destinationAddress);

            return trHash;
        }

        public async Task<string> GetUserAddress(string contractAddress)
        {
            return (await _contractRepository.GetByContractAddress(contractAddress)).UserAddress;
        }
    }

    public interface IErc20DepositContractService
    {
        Task<string> AssignContract(string userAddress);

        Task<string> CreateContract();

        Task<IEnumerable<string>> GetContractAddresses(IEnumerable<string> txHashes);

        Task<string> GetContractAddress(string userAddress);

        Task<string> GetUserAddress(string contractUser);

        Task ProcessAllAsync(Func<IErc20DepositContract, Task> processAction);

        Task<string> RecievePaymentFromDepositContract(string depositContractAddress,
           string erc20TokenAddress, string destinationAddress);
    }
}
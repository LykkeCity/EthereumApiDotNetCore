using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Common.Log;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;

namespace Lykke.Service.EthereumCore.Services.LykkePay
{
    //LykkePay Erc20 deposit contract service
    public class LykkePayErc20DepositContractService : IErc20DepositContractService
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

        private readonly IErc223DepositContractRepository _contractRepository;
        private readonly IContractService _contractService;
        private readonly IErc20DepositContractQueueServiceFactory _poolFactory;
        private readonly IBaseSettings _settings;
        private readonly ILog _log;
        private readonly IWeb3 _web3;

        public LykkePayErc20DepositContractService(
            [KeyFilter(Constants.LykkePayKey)] IErc223DepositContractRepository contractRepository,
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
                var pool = _poolFactory.Get(Constants.LykkePayErc20DepositContractPoolQueue);

                contractAddress = await pool.GetContractAddress();

                await _contractRepository.AddOrReplace(new Erc20DepositContract
                {
                    ContractAddress = contractAddress,
                    UserAddress = userAddress
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
            var contract = await _contractRepository.Get(userAddress);

            return contract?.ContractAddress;
        }

        public async Task ProcessAllAsync(Func<IErc20DepositContract, Task> processAction)
        {
            await _contractRepository.ProcessAllAsync(processAction);
        }

        public async Task<bool> ContainsAsync(string address)
        {
            var contains = await _contractRepository.Contains(address);

            return contains;
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
            var cashinWouldBeSuccesfull = await cashin.CallAsync<bool>(_settings.EthereumMainAccount,
            new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0), erc20TokenAddress, destinationAddress);

            if (!cashinWouldBeSuccesfull)
            {
                throw new Exception($"CAN'T Estimate Cashin {depositContractAddress}, {erc20TokenAddress}, {destinationAddress}");
            }

            string trHash = await cashin.SendTransactionAsync(_settings.EthereumMainAccount,
            new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0), erc20TokenAddress, destinationAddress);

            return trHash;
        }

        public async Task<string> GetUserAddress(string contractAddress)
        {
            var contract = (await _contractRepository.GetByContractAddress(contractAddress));

            return contract?.UserAddress;
        }

        private async Task<string> StartTransferAsync(string depositContractAddress,
            string erc20TokenAddress, string destinationAddress)
        {
            Contract contract = _web3.Eth.GetContract(_settings.Erc20DepositContract.Abi, depositContractAddress);
            var cashin = contract.GetFunction("transferAllTokens");
            var cashinWouldBeSuccesfull = await cashin.CallAsync<bool>(_settings.EthereumMainAccount,
                new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0), erc20TokenAddress, destinationAddress);

            if (!cashinWouldBeSuccesfull)
            {
                throw new Exception($"CAN'T Estimate Cashin {depositContractAddress}, {erc20TokenAddress}, {destinationAddress}");
            }

            string trHash = await cashin.SendTransactionAsync(_settings.EthereumMainAccount,
                new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0), erc20TokenAddress, destinationAddress);

            return trHash;
        }
    }
}
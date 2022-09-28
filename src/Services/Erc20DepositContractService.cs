using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.EthereumCore.Contracts.Cqrs;
using Lykke.Job.EthereumCore.Workflow.Commands;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Common;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;

namespace Lykke.Service.EthereumCore.Services
{
    public class Erc20ContracAssigner : IErc20ContracAssigner
    {
        private readonly IErc20DepositContractQueueServiceFactory _poolFactory;
        private readonly IErc223DepositContractRepository _contractRepository;
        private readonly ICqrsEngine _cqrsEngine;

        public Erc20ContracAssigner(IErc20DepositContractQueueServiceFactory poolFactory,
            [KeyFilter(Constants.DefaultKey)]IErc223DepositContractRepository contractRepository,
            ICqrsEngine cqrsEngine)
        {
            _cqrsEngine = cqrsEngine;
            _poolFactory = poolFactory;
            _contractRepository = contractRepository;
        }

        public async Task<string> AssignContract(string userAddress)
        {
            var contractAddress = await GetContractAddress(userAddress);

            if (string.IsNullOrEmpty(contractAddress))
            {
                var pool = _poolFactory.Get(Constants.Erc20DepositContractPoolQueue);

                contractAddress = await pool.GetContractAddress();

                var command = new AssignErc223DepositToUserCommand()
                {
                    UserAddress = userAddress,
                    ContractAddress = contractAddress
                };

                try
                {
                    _cqrsEngine.SendCommand(command,
                        "blockchain.ethereum.core.api",
                        EthereumBoundedContext.Name);
                }
                catch (Exception e)
                {
                    await pool.PushContractAddress(contractAddress);

                    throw;
                }
            }

            return contractAddress;
        }

        public async Task<string> GetContractAddress(string userAddress)
        {
            var contract = await _contractRepository.Get(userAddress);

            return contract?.ContractAddress;
        }
    }

    //Default Erc20 deposit contract
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

        private readonly IErc20DepositContractRepositoryOld _oldContractRepository;
        private readonly IErc223DepositContractRepository _contractRepository;
        private readonly IContractService _contractService;
        private readonly IErc20DepositContractQueueServiceFactory _poolFactory;
        private readonly IBaseSettings _settings;
        private readonly ILog _log;
        private readonly IWeb3 _web3;
        private readonly ICqrsEngine _cqrsEngine;

        public Erc20DepositContractService(
            IErc20DepositContractRepositoryOld oldContractRepository,
            [KeyFilter(Constants.DefaultKey)]IErc223DepositContractRepository contractRepository,
            IContractService contractService,
            IErc20DepositContractQueueServiceFactory poolFactory,
            IBaseSettings settings,
            ILog log,
            IWeb3 web3)
        {
            _oldContractRepository = oldContractRepository;
            _contractRepository = contractRepository;
            _contractService = contractService;
            _poolFactory = poolFactory;
            _settings = settings;
            _log = log;
            _web3 = web3;
        }


        public async Task<string> AssignContract(string userAddress)
        {
            throw new NotImplementedException();
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
            await _oldContractRepository.ProcessAllAsync(processAction);
        }

        public async Task<bool> ContainsAsync(string address)
        {
            var contains = await _contractRepository.Contains(address) || await _oldContractRepository.Contains(address);

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

            _log.WriteInfoAsync(nameof(Erc20DepositContractService), nameof(RecievePaymentFromDepositContract),
                new
                {
                    _settings.GasForHotWalletTransaction,
                    TokenAddress = erc20TokenAddress,
                    DestinationAddress = destinationAddress
                }.ToJson(),
                "Receiving payment from deposit contract (estimation)");
            
            var cashinWouldBeSuccesfull = await cashin.CallAsync<bool>(_settings.EthereumMainAccount,
            new HexBigInteger(_settings.GasForHotWalletTransaction), new HexBigInteger(0), erc20TokenAddress, destinationAddress);

            if (!cashinWouldBeSuccesfull)
            {
                throw new ClientSideException(ExceptionType.CantEstimateExecution, $"CAN'T Estimate Cashin {depositContractAddress}, {erc20TokenAddress}, {destinationAddress}");
            }

            _log.WriteInfoAsync(nameof(Erc20DepositContractService), nameof(RecievePaymentFromDepositContract),
                new
                {
                    _settings.GasForHotWalletTransaction,
                    TokenAddress = erc20TokenAddress,
                    DestinationAddress = destinationAddress
                }.ToJson(),
                "Receiving payment from deposit contract (sending transaction)");
            
            string trHash = await cashin.SendTransactionAsync(_settings.EthereumMainAccount,
            new HexBigInteger(_settings.GasForHotWalletTransaction), new HexBigInteger(0), erc20TokenAddress, destinationAddress);

            return trHash;
        }

        public async Task<string> GetUserAddress(string contractAddress)
        {
            var contract = (await _contractRepository.GetByContractAddress(contractAddress)) ??
                (await _oldContractRepository.GetByContractAddress(contractAddress));

            return contract.UserAddress;
        }
    }

    public interface IErc20DepositContractService : IErc20DepositContractLocatorService
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

    public interface IErc20ContracAssigner
    {
        Task<string> AssignContract(string userAddress);
    }
}
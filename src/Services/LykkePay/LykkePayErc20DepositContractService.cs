using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using AzureStorage.Queue;
using Common.Log;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Core.Shared;
using Nethereum.Web3;

namespace Lykke.Service.EthereumCore.Services.LykkePay
{
    public class LykkePayErc20DepositContractService : IErc20DepositContractService
    {
        private readonly IErc223DepositContractRepository _contractRepository;
        private readonly IContractService _contractService;
        private readonly IErc20DepositContractQueueServiceFactory _poolFactory;
        private readonly IBaseSettings _settings;
        private readonly ILog _log;
        private readonly IWeb3 _web3;
        private readonly AppSettings _appSettings;
        private readonly IQueueExt _transferQueue;
        private readonly IErcInterfaceService _ercInterfaceService;
        private readonly IHotWalletOperationRepository _operationsRepository;
        private readonly IUserTransferWalletRepository _userTransferWalletRepository;

        public LykkePayErc20DepositContractService(
            [KeyFilter(Constants.LykkePayKey)] IErc223DepositContractRepository contractRepository,
            [KeyFilter(Constants.LykkePayKey)] IHotWalletOperationRepository operationsRepository,
            IContractService contractService,
            IErc20DepositContractQueueServiceFactory poolFactory,
            IBaseSettings settings,
            ILog log,
            IWeb3 web3,
            AppSettings appSettings,
            IQueueFactory factory,
            IErcInterfaceService ercInterfaceService,
            IUserTransferWalletRepository userTransferWalletRepository)
        {
            _contractRepository = contractRepository;
            _contractService = contractService;
            _poolFactory = poolFactory;
            _settings = settings;
            _log = log;
            _web3 = web3;
            _appSettings = appSettings;
            _transferQueue = factory.Build(Constants.LykkePayErc223TransferQueue);
            _ercInterfaceService = ercInterfaceService;
            _operationsRepository = operationsRepository;
            _userTransferWalletRepository = userTransferWalletRepository;
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

            var fromAddress = _appSettings.LykkePay.LykkePayAddress;
            var abi = _settings.Erc20DepositContract.Abi;
            var byteCode = _settings.Erc20DepositContract.ByteCode;

            return await _contractService.CreateContractWithoutBlockchainAcceptanceFromSpecificAddress(fromAddress, abi, byteCode);
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
            var depositContract = await _contractRepository.GetByContractAddress(depositContractAddress);
            if (depositContract == null)
            {
                throw new ClientSideException(ExceptionType.WrongParams, $"DepositContractAddress {depositContractAddress} does not exist");
            }

            var userWallet = await TransferWalletSharedService.GetUserTransferWalletAsync(_userTransferWalletRepository,
                depositContractAddress, erc20TokenAddress, depositContract.UserAddress);

            if (userWallet != null && !string.IsNullOrEmpty(userWallet.LastBalance))
            {
                throw new ClientSideException(ExceptionType.TransferInProcessing, $"Transfer from {depositContractAddress} was started before wait for it to complete");
            }

            var balance = await _ercInterfaceService.GetBalanceForExternalTokenAsync(depositContractAddress, erc20TokenAddress);
            if (balance == 0)
            {
                throw new ClientSideException(ExceptionType.NotEnoughFunds, $"No tokens detected at deposit address {depositContractAddress}");
            }

            var guidStr = Guid.NewGuid().ToString();

            var message = new Lykke.Service.EthereumCore.Core.Messages.LykkePay.LykkePayErc20TransferMessage()
            {
                OperationId = guidStr
            };

            var existingOperation = await _operationsRepository.GetAsync(guidStr);
            if (existingOperation != null)
            {
                throw new ClientSideException(ExceptionType.EntityAlreadyExists, "Try again later");
            }
            var transactionSenderAddress = _appSettings.LykkePay.LykkePayAddress;
            var estimationResult = await Erc20SharedService.EstimateDepositTransferAsync(_web3,
                _settings.Erc20DepositContract.Abi,
                transactionSenderAddress,
                depositContractAddress,
                erc20TokenAddress, 
                destinationAddress,
                _settings,
                _log);

            if (!estimationResult)
            {
                throw new ClientSideException(ExceptionType.WrongDestination, $"Can't estimate transfer {depositContractAddress}, {erc20TokenAddress}, {destinationAddress}");
            }

            await _operationsRepository.SaveAsync(new HotWalletOperation()
            {
                Amount = balance,
                FromAddress = depositContractAddress,
                OperationId = guidStr,
                OperationType = HotWalletOperationType.Cashin,
                ToAddress = destinationAddress,
                TokenAddress = erc20TokenAddress
            });

            await _transferQueue.PutRawMessageAsync(Newtonsoft.Json.JsonConvert.SerializeObject(message));

            await TransferWalletSharedService.UpdateUserTransferWalletAsync(_userTransferWalletRepository,
                depositContractAddress, erc20TokenAddress, depositContract.UserAddress, balance.ToString());

            return guidStr;
        }

        public async Task<string> GetUserAddress(string contractAddress)
        {
            var contract = await _contractRepository.GetByContractAddress(contractAddress);

            return contract?.UserAddress;
        }
    }
}
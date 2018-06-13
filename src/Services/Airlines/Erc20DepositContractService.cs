using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using AzureStorage.Queue;
using Common.Log;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Airlines;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Services;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Core.Shared;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;

namespace Lykke.Service.EthereumCore.Services.Airlines
{
    public class Erc20DepositContractService : IAirlinesErc20DepositContractService
    {
        private readonly IErc223DepositContractRepository _contractRepository;
        private readonly IContractService _contractService;
        private readonly IErc20DepositContractQueueServiceFactory _poolFactory;
        private readonly AirlinesSettings _settings;
        private readonly ILog _log;
        private readonly IWeb3 _web3;
        private readonly AirlinesAppSettings _appSettings;
        private readonly IQueueExt _transferQueue;
        private readonly IErcInterfaceService _ercInterfaceService;
        private readonly IHotWalletOperationRepository _operationsRepository;
        private readonly IUserTransferWalletRepository _userTransferWalletRepository;

        public Erc20DepositContractService(
            [KeyFilter(Constants.AirLinesKey)] IErc223DepositContractRepository contractRepository,
            [KeyFilter(Constants.AirLinesKey)] IHotWalletOperationRepository operationsRepository,
            IContractService contractService,
            IErc20DepositContractQueueServiceFactory poolFactory,
            AirlinesSettings settings,
            ILog log,
            IWeb3 web3,
            AirlinesAppSettings appSettings,
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
            try
            {
                var fromAddress = _appSettings.Airlines.AirlinesAddress;
                var abi = _settings.Erc223DepositContract.Abi;
                var byteCode = _settings.Erc223DepositContract.ByteCode;

                return await _contractService.CreateContractWithoutBlockchainAcceptanceFromSpecificAddress(fromAddress, abi, byteCode);
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(Services.Erc20DepositContractService), nameof(CreateContract), "", e, DateTime.UtcNow);

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
           string erc20TokenAddress, string destinationAddress, string tokenAmount)
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

            var amount = System.Numerics.BigInteger.Parse(tokenAmount);
            var balance = await _ercInterfaceService.GetBalanceForExternalTokenAsync(depositContractAddress, erc20TokenAddress);
            if (balance == 0 || amount > balance)
            {
                throw new ClientSideException(ExceptionType.NotEnoughFunds, 
                    $"Not enough tokens to proceed with withdrawal detected at deposit address {depositContractAddress}. " +
                    $"Current balance: {balance}");
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
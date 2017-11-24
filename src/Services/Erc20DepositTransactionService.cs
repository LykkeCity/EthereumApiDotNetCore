using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Repositories;
using Core.Settings;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Services.Coins;
using AzureStorage.Queue;
using Common.Log;
using System.Numerics;
using Core.Utils;
using Services.HotWallet;

namespace Services
{
    public class Erc20DepositContractTransaction : QueueMessageBase
    {
        public string ContractAddress { get; set; }
        public string UserAddress { get; set; }
        public string TokenAddress { get; set; }
        public string Amount { get; set; }
        public DateTime CreateDt { get; set; }
    }

    public interface IErc20DepositTransactionService
    {
        Task PutContractTransferTransaction(Erc20DepositContractTransaction tr);
        Task TransferToCoinContract(Erc20DepositContractTransaction contractTransferTr);
    }

    public class Erc20DepositTransactionService : IErc20DepositTransactionService
    {
        private readonly ILog _logger;
        private readonly IBaseSettings _baseSettings;
        private readonly IQueueExt _queue;
        private readonly IErc20DepositContractService _erc20DepositContractService;
        private readonly ITransferContractRepository _transferContractRepository;
        private TransferContractService _transferContractService;
        private readonly IUserTransferWalletRepository _userTransferWalletRepository;
        private readonly IUserPaymentHistoryRepository _userPaymentHistoryRepository;
        private readonly ICoinTransactionService _cointTransactionService;
        private readonly ICoinTransactionRepository _coinTransactionRepository;
        private readonly ICoinEventService _coinEventService;
        private readonly IErcInterfaceService _ercInterfaceService;
        private readonly IEventTraceRepository _eventTraceRepository;
        private readonly string _hotWalletAddress;
        private readonly IHotWalletService _hotWalletService;

        public Erc20DepositTransactionService(IQueueFactory queueFactory,
            ILog logger,
            IExchangeContractService coinContractService,
            IBaseSettings baseSettings,
            IErc20DepositContractService erc20DepositContractService,
            TransferContractService transferContractService,
            IUserTransferWalletRepository userTransferWalletRepository,
            IUserPaymentHistoryRepository userPaymentHistoryRepository,
            ICoinTransactionService cointTransactionService,
            ICoinTransactionRepository coinTransactionRepository,
            ICoinEventService coinEventService,
            IEventTraceRepository eventTraceRepository,
            IErcInterfaceService ercInterfaceService,
            SettingsWrapper settingsWrapper,
            IHotWalletService hotWalletService)
        {
            _eventTraceRepository = eventTraceRepository;
            _logger = logger;
            _baseSettings = baseSettings;
            _queue = queueFactory.Build(Constants.Erc20DepositCashinTransferQueue);
            _erc20DepositContractService = erc20DepositContractService;
            _transferContractService = transferContractService;
            _userTransferWalletRepository = userTransferWalletRepository;
            _userPaymentHistoryRepository = userPaymentHistoryRepository;
            _cointTransactionService = cointTransactionService;
            _coinTransactionRepository = coinTransactionRepository;
            _coinEventService = coinEventService;
            _ercInterfaceService = ercInterfaceService;
            _hotWalletAddress = settingsWrapper.Ethereum.HotwalletAddress;
            _hotWalletService = hotWalletService;
        }

        public async Task PutContractTransferTransaction(Erc20DepositContractTransaction tr)
        {
            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(tr));
        }

        public async Task TransferToCoinContract(Erc20DepositContractTransaction contractTransferTr)
        {
            try
            {
                var userAddress = contractTransferTr.UserAddress;

                if (string.IsNullOrEmpty(userAddress) || userAddress == Constants.EmptyEthereumAddress)
                {
                    await UpdateUserTransferWallet(contractTransferTr);
                    await _logger.WriteInfoAsync("TransferContractTransactionService", "TransferToCoinContract", "",
                        $"Can't cashin: there is no user assigned to the transfer contract {contractTransferTr.ContractAddress}", DateTime.UtcNow);

                    return;
                }

                var tokenAddress = contractTransferTr.TokenAddress;
                var contractAddress = await _erc20DepositContractService.GetContractAddress(contractTransferTr.UserAddress);
                var balance = await _ercInterfaceService.GetBalanceForExternalTokenAsync(contractTransferTr.ContractAddress, contractTransferTr.TokenAddress);

                if (balance == 0)
                {
                    await UpdateUserTransferWallet(contractTransferTr);
                    await _logger.WriteInfoAsync("TransferContractTransactionService", "TransferToCoinContract", "",
                        $"Can't cashin: there is no funds on the transfer contract {contractTransferTr.ContractAddress}", DateTime.UtcNow);

                    return;
                }

                var opId = $"HotWalletCashin-{Guid.NewGuid().ToString()}";

                var transactionHash = await _hotWalletService.StartCashinAsync(new HotWalletOperation()
                {
                    Amount = balance,
                    FromAddress = contractAddress,
                    OperationId = opId,
                    ToAddress = _hotWalletAddress,
                    TokenAddress = tokenAddress,
                    OperationType = HotWalletOperationType.Cashin,
                });

                await _userPaymentHistoryRepository.SaveAsync(new UserPaymentHistory()
                {
                    Amount = balance.ToString(),
                    ToAddress = contractAddress,
                    AdapterAddress = $"HotWallet-Token-{tokenAddress}",
                    CreatedDate = DateTime.UtcNow,
                    Note = $"Cashin from erc20 deposit contract {contractAddress}",
                    TransactionHash = transactionHash,
                    UserAddress = contractTransferTr.UserAddress
                });

                //await UpdateUserTransferWallet(contractTransferTr);
                await _logger.WriteInfoAsync(nameof(Erc20DepositTransactionService), nameof(TransferToCoinContract), "",
                    $"Transfered {balance} from erc 20 deposit contract to {_hotWalletAddress} by transaction {transactionHash}. " +
                    $"Receiver = {userAddress}");
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync(nameof(Erc20DepositTransactionService), nameof(TransferToCoinContract),
                            $"{contractTransferTr.ContractAddress} - erc20 - {contractTransferTr.TokenAddress} - {contractTransferTr.Amount}", e);
                throw;
            }
        }

        private async Task UpdateUserTransferWallet(Erc20DepositContractTransaction contractTransferTr)
        {
            string formattedAddress =
                _userTransferWalletRepository.FormatAddressForErc20(contractTransferTr.ContractAddress, contractTransferTr.TokenAddress);

            await _userTransferWalletRepository.ReplaceAsync(new UserTransferWallet()
            {
                LastBalance = "",
                TransferContractAddress = formattedAddress,
                UpdateDate = DateTime.UtcNow,
                UserAddress = contractTransferTr.UserAddress
            });
        }
    }
}

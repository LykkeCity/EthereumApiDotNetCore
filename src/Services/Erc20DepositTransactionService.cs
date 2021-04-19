using System;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Newtonsoft.Json;
using AzureStorage.Queue;
using Common.Log;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.Notifiers;
using Lykke.Service.EthereumCore.Core.Utils;
using Lykke.Service.EthereumCore.Services.HotWallet;
using Lykke.Service.EthereumCore.Services.Coins.Models;

namespace Lykke.Service.EthereumCore.Services
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
        private readonly IQueueExt _queue;
        private readonly ITransferContractRepository _transferContractRepository;
        private readonly IUserTransferWalletRepository _userTransferWalletRepository;
        private readonly IUserPaymentHistoryRepository _userPaymentHistoryRepository;
        private readonly IErcInterfaceService _ercInterfaceService;
        private readonly string _hotWalletAddress;
        private readonly IHotWalletService _hotWalletService;
        private readonly IQueueExt _cointTransactionQueue;
        private readonly ISlackNotifier _slackNotifier;

        public Erc20DepositTransactionService(IQueueFactory queueFactory,
            ILog logger,
            IUserTransferWalletRepository userTransferWalletRepository,
            IUserPaymentHistoryRepository userPaymentHistoryRepository,
            IErcInterfaceService ercInterfaceService,
            AppSettings settingsWrapper,
            IHotWalletService hotWalletService,
            ISlackNotifier slackNotifier)
        {
            _logger = logger;
            _queue = queueFactory.Build(Constants.Erc20DepositCashinTransferQueue);
            _userTransferWalletRepository = userTransferWalletRepository;
            _userPaymentHistoryRepository = userPaymentHistoryRepository;
            _ercInterfaceService = ercInterfaceService;
            _hotWalletAddress = settingsWrapper.Ethereum.HotwalletAddress;
            _hotWalletService = hotWalletService;
            _cointTransactionQueue = queueFactory.Build(Constants.HotWalletTransactionMonitoringQueue);
            _slackNotifier = slackNotifier;
        }

        public async Task PutContractTransferTransaction(Erc20DepositContractTransaction tr)
        {
            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(tr));
        }

        public async Task TransferToCoinContract(Erc20DepositContractTransaction contractTransferTr)
        {
            try
            {
                var tokenAddress = contractTransferTr.TokenAddress;
                var contractAddress = contractTransferTr.ContractAddress;
                var userAddress = contractTransferTr.UserAddress;

                if (string.IsNullOrEmpty(userAddress) || userAddress == Constants.EmptyEthereumAddress)
                {
                    await UpdateUserTransferWallet(contractTransferTr);
                    await _logger.WriteInfoAsync("TransferContractTransactionService", "TransferToCoinContract", "",
                        $"Can't cashin: there is no user assigned to the transfer contract {contractTransferTr.ContractAddress}", DateTime.UtcNow);

                    return;
                }

                if (string.IsNullOrEmpty(contractAddress) || contractAddress == Constants.EmptyEthereumAddress)
                {
                    await UpdateUserTransferWallet(contractTransferTr);
                    await _logger.WriteInfoAsync("TransferContractTransactionService", "TransferToCoinContract", "",
                        $"Can't cashin: there is no contract address in message{contractTransferTr?.ToJson()}", DateTime.UtcNow);

                    return;
                }

                var balance = await _ercInterfaceService.GetBalanceForExternalTokenAsync(contractAddress, contractTransferTr.TokenAddress);

                if (balance == 0)
                {
                    await UpdateUserTransferWallet(contractTransferTr);
                    await _logger.WriteInfoAsync("TransferContractTransactionService", "TransferToCoinContract", "",
                        $"Can't cashin: there is no funds on the transfer contract {contractAddress}", DateTime.UtcNow);

                    return;
                }

                var opId = $"HotWalletCashin-{Guid.NewGuid().ToString()}";
                string transactionHash = null; 

                try
                {
                    transactionHash = await _hotWalletService.StartCashinAsync(new HotWalletOperation()
                    {
                        Amount = balance,
                        FromAddress = contractAddress,
                        OperationId = opId,
                        ToAddress = _hotWalletAddress,
                        TokenAddress = tokenAddress,
                        OperationType = HotWalletOperationType.Cashin,
                    });

                    await _cointTransactionQueue.PutRawMessageAsync(JsonConvert.SerializeObject(new CoinTransactionMessage() { TransactionHash = transactionHash }));
                }
                catch (ClientSideException clientSideExc)
                {
                    var context = new
                    {
                        obj = contractTransferTr.ToJson(),
                        exc = $"{clientSideExc.ExceptionType} {clientSideExc.Message} {clientSideExc.StackTrace}"
                    }.ToJson();
                    await _logger.WriteInfoAsync(nameof(Erc20DepositTransactionService), nameof(TransferToCoinContract),
                        $"{context}");
                    await UpdateUserTransferWallet(contractTransferTr);
                    //Redirect issues to dedicated slack channel
                    await _slackNotifier.ErrorAsync($"{nameof(Erc20DepositTransactionService)} can't start cashin {context}");

                    return;
                }
                catch (Exception exc)
                {
                    await _logger.WriteErrorAsync(nameof(Erc20DepositTransactionService), nameof(TransferToCoinContract),
                            $"{contractTransferTr.ToJson()}", exc);
                    await UpdateUserTransferWallet(contractTransferTr);

                    return;
                }

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

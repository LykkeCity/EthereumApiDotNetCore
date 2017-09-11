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

namespace Services
{
    public class TransferContractTransaction : QueueMessageBase
    {
        public string ContractAddress { get; set; }

        public string UserAddress { get; set; }
        public string CoinAdapterAddress { get; set; }

        //System.Numerics.BigInteger
        public string Amount { get; set; }
        public DateTime CreateDt { get; set; }

    }

    public interface ITransferContractTransactionService
    {
        Task PutContractTransferTransaction(TransferContractTransaction tr);
        Task TransferToCoinContract(TransferContractTransaction contractTransferTr);
    }

    public class TransferContractTransactionService : ITransferContractTransactionService
    {
        private readonly ILog _logger;
        private readonly IBaseSettings _baseSettings;
        private readonly IQueueExt _queue;
        private readonly ITransferContractRepository _transferContractRepository;
        private TransferContractService _transferContractService;
        private readonly IUserTransferWalletRepository _userTransferWalletRepository;
        private readonly IUserPaymentHistoryRepository _userPaymentHistoryRepository;
        private readonly ICoinTransactionService _cointTransactionService;
        private readonly ICoinTransactionRepository _coinTransactionRepository;
        private readonly ICoinEventService _coinEventService;
        private readonly IEventTraceRepository _eventTraceRepository;

        public TransferContractTransactionService(Func<string, IQueueExt> queueFactory,
            ILog logger,
            IExchangeContractService coinContractService,
            IBaseSettings baseSettings,
            ITransferContractRepository transferContractRepository,
            TransferContractService transferContractService,
            IUserTransferWalletRepository userTransferWalletRepository,
            IUserPaymentHistoryRepository userPaymentHistoryRepository,
            ICoinTransactionService cointTransactionService,
            ICoinTransactionRepository coinTransactionRepository,
            ICoinEventService coinEventService,
            IEventTraceRepository eventTraceRepository)
        {
            _eventTraceRepository = eventTraceRepository;
            _logger = logger;
            _baseSettings = baseSettings;
            _queue = queueFactory(Constants.ContractTransferQueue);
            _transferContractRepository = transferContractRepository;
            _transferContractService = transferContractService;
            _userTransferWalletRepository = userTransferWalletRepository;
            _userPaymentHistoryRepository = userPaymentHistoryRepository;
            _cointTransactionService = cointTransactionService;
            _coinTransactionRepository = coinTransactionRepository;
            _coinEventService = coinEventService;
        }

        public async Task PutContractTransferTransaction(TransferContractTransaction tr)
        {
            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(tr));
        }

        public async Task TransferToCoinContract(TransferContractTransaction contractTransferTr)
        {
            try
            {
                var contractEntity = await _transferContractRepository.GetAsync(contractTransferTr.ContractAddress);
                var balance = await _transferContractService.GetBalance(contractTransferTr.ContractAddress);

                if (balance == 0)
                {
                    await UpdateUserTransferWallet(contractTransferTr);
                    await _logger.WriteInfoAsync("TransferContractTransactionService", "TransferToCoinContract", "", 
                        $"Can't cashin: there is no funds on the transfer contract {contractTransferTr.ContractAddress}", DateTime.UtcNow);

                    return;
                }

                var userAddress = await _transferContractService.GetUserAddressForTransferContract(contractTransferTr.ContractAddress);
                if (string.IsNullOrEmpty(userAddress) || userAddress == Constants.EmptyEthereumAddress)
                {
                    await UpdateUserTransferWallet(contractTransferTr);
                    await _logger.WriteInfoAsync("TransferContractTransactionService", "TransferToCoinContract", "",
                        $"Can't cashin: there is no user assigned to the transfer contract {contractTransferTr.ContractAddress}", DateTime.UtcNow);

                    return;
                }

                    var opId = $"InternalOperation-{Guid.NewGuid().ToString()}";
                var transactionHash = await _transferContractService.RecievePaymentFromTransferContract(contractEntity.ContractAddress, contractEntity.CoinAdapterAddress);
                await _coinEventService.PublishEvent(new CoinEvent(opId, 
                    transactionHash, contractTransferTr.ContractAddress, contractTransferTr.UserAddress,
                    balance.ToString(), CoinEventType.CashinStarted, contractEntity.CoinAdapterAddress));
                await _eventTraceRepository.InsertAsync(new EventTrace()
                {
                    Note = $"First Cashin appearance {transactionHash} put in {Constants.TransactionMonitoringQueue}",
                    OperationId = opId,
                    TraceDate = DateTime.UtcNow
                });
                await _userPaymentHistoryRepository.SaveAsync(new UserPaymentHistory()
                {
                    Amount = balance.ToString(),
                    ToAddress = contractEntity.ContractAddress,
                    AdapterAddress = contractEntity.CoinAdapterAddress,
                    CreatedDate = DateTime.UtcNow,
                    Note = $"Cashin from transfer contract {contractEntity.ContractAddress}",
                    TransactionHash = transactionHash,
                    UserAddress = contractTransferTr.UserAddress
                });

                //await UpdateUserTransferWallet(contractTransferTr);
                await _logger.WriteInfoAsync("ContractTransferTransactionService", "TransferToCoinContract", "",
                    $"Transfered {balance} from transfer contract to \"{contractTransferTr.CoinAdapterAddress}\" by transaction \"{transactionHash}\". Receiver = {contractEntity.UserAddress}");
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync("TransferContractTransactionService", "TransferToCoinContract",
                            $"{contractTransferTr.ContractAddress} - {contractTransferTr.CoinAdapterAddress} - {contractTransferTr.Amount}", e);
                throw;
            }
        }

        private async Task UpdateUserTransferWallet(TransferContractTransaction contractTransferTr)
        {
            await _userTransferWalletRepository.ReplaceAsync(new UserTransferWallet()
            {
                LastBalance = "",
                TransferContractAddress = contractTransferTr.ContractAddress,
                UpdateDate = DateTime.UtcNow,
                UserAddress = contractTransferTr.UserAddress
            });
        }
    }
}

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

namespace Services
{
    public class TransferContractTransaction
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
        Task<bool> CompleteTransfer();
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

        public TransferContractTransactionService(Func<string, IQueueExt> queueFactory,
            ILog logger,
            IExchangeContractService coinContractService,
            IBaseSettings baseSettings,
            ITransferContractRepository transferContractRepository,
            TransferContractService transferContractService,
            IUserTransferWalletRepository userTransferWalletRepository,
            IUserPaymentHistoryRepository userPaymentHistoryRepository)
        {
            _logger = logger;
            _baseSettings = baseSettings;
            _queue = queueFactory(Constants.ContractTransferQueue);
            _transferContractRepository = transferContractRepository;
            _transferContractService = transferContractService;
            _userTransferWalletRepository = userTransferWalletRepository;
            _userPaymentHistoryRepository = userPaymentHistoryRepository;
        }

        public async Task PutContractTransferTransaction(TransferContractTransaction tr)
        {
            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(tr));
        }

        public async Task<bool> CompleteTransfer()
        {
            var item = await _queue.GetRawMessageAsync();

            if (item == null)
                return false;

            var contractTransferTr = JsonConvert.DeserializeObject<TransferContractTransaction>(item.AsString);

            await TransferToCoinContract(item, contractTransferTr);

            await _queue.FinishRawMessageAsync(item);
            return true;
        }

        private async Task TransferToCoinContract(CloudQueueMessage item, TransferContractTransaction contractTransferTr)
        {
            try
            {
                var amount = BigInteger.Parse(contractTransferTr.Amount);
                var contractEntity = await _transferContractRepository.GetAsync(contractTransferTr.ContractAddress);
                var balance = await _transferContractService.GetBalance(contractTransferTr.CoinAdapterAddress, contractTransferTr.UserAddress);
                var tr = await _transferContractService.RecievePaymentFromTransferContract(contractEntity.ContractAddress,
                    contractEntity.CoinAdapterAddress, contractTransferTr.UserAddress);

                await _userPaymentHistoryRepository.SaveAsync(new UserPaymentHistory() {
                    Amount = balance.ToString(),
                    ContractAddress = contractEntity.ContractAddress,
                    AdapterAddress = contractEntity.CoinAdapterAddress,
                    CreatedDate = DateTime.UtcNow,
                    Note= $"Cashin from transfer contract {contractEntity.ContractAddress}",
                    TransactionHash= tr,
                    UserAddress = contractTransferTr.UserAddress
                });
                await _userTransferWalletRepository.ReplaceAsync(new UserTransferWallet()
                {
                    LastBalance = "",
                    TransferContractAddress = contractTransferTr.ContractAddress,
                    UpdateDate = DateTime.UtcNow,
                    UserAddress = contractTransferTr.UserAddress
                });
                await _logger.WriteInfoAsync("ContractTransferTransactionService", "TransferToCoinContract", "",
                    $"Transfered {contractTransferTr.Amount} Eth from transfer contract to \"{_baseSettings.EthCoin}\" by transaction \"{tr}\". Receiver = {contractEntity.CoinAdapterAddress}");
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync("TransferContractTransactionService", "TransferToCoinContract",
                            $"{contractTransferTr.ContractAddress} - {contractTransferTr.CoinAdapterAddress} - {contractTransferTr.Amount}", e);
            }
        }
    }
}

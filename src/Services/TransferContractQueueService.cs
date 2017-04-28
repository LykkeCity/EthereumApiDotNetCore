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
        public BigInteger Amount { get; set; }
        public DateTime CreateDt { get; set; }
    }

    public interface ITransferContractTransactionService
    {
        Task PutContractTransferTransaction(TransferContractTransaction tr);
        Task<bool> CompleteTransfer();
    }

    public class TransferContractTransactionService : ITransferContractTransactionService
    {
        private readonly IEthereumQueueOutService _queueOutService;
        private readonly IEthereumTransactionService _ethereumTransactionService;
        private readonly ILog _logger;
        private readonly IBaseSettings _baseSettings;
        private readonly IQueueExt _queue;
        private readonly ITransferContractRepository _transferContractRepository;
        private TransferContractService _transferContractService;
        private readonly IUserTransferWalletRepository _userTransferWalletRepository;

        public TransferContractTransactionService(Func<string, IQueueExt> queueFactory,
            IEthereumQueueOutService queueOutService,
            IEthereumTransactionService ethereumTransactionService,
            ILog logger,
            ICoinContractService coinContractService,
            IBaseSettings baseSettings,
            ITransferContractRepository transferContractRepository,
            TransferContractService transferContractService,
            IUserTransferWalletRepository userTransferWalletRepository)
        {
            _logger = logger;
            _baseSettings = baseSettings;
            _queue = queueFactory(Constants.ContractTransferQueue);
            _transferContractRepository = transferContractRepository;
            _transferContractService = transferContractService;
            _userTransferWalletRepository = userTransferWalletRepository;
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
                var contractEntity = await _transferContractRepository.GetAsync(contractTransferTr.ContractAddress);

                var tr = await _transferContractService.RecievePaymentFromTransferContract(Guid.Parse(item.Id), contractEntity.ContractAddress,
                    contractEntity.CoinAdapterAddress, contractTransferTr.Amount, contractEntity.ContainsEth);
                await _userTransferWalletRepository.ReplaceAsync(new UserTransferWallet()
                {
                    LastBalance = 0,
                    TransferContractAddress = contractTransferTr.ContractAddress,
                    UpdateDate = DateTime.UtcNow,
                    UserAddress = 
                });
                await _logger.WriteInfoAsync("ContractTransferTransactionService", "TransferToCoinContract", "",
                    $"Transfered {contractTransferTr.Amount} Eth from transfer contract to \"{_baseSettings.EthCoin}\" by transaction \"{tr}\". Receiver = {contractEntity.UserWallet}");
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync("TransferContractTransactionService", "TransferToCoinContract",
                            $"{contractTransferTr.ContractAddress} - {contractTransferTr.CoinAdapterAddress} - {contractTransferTr.Amount}", e);
            }
        }
    }
}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Core;
//using Core.Repositories;
//using Core.Settings;
//using Microsoft.WindowsAzure.Storage.Queue;
//using Newtonsoft.Json;
//using Services.Coins;
//using AzureStorage.Queue;
//using Common.Log;

//namespace Services
//{
//    public class ContractTransferTransaction
//    {
//        public string TransactionHash { get; set; }

//        public string Contract { get; set; }

//        public decimal Amount { get; set; }

//        public DateTime CreateDt { get; set; }
//    }


//    public interface IContractTransferTransactionService
//    {
//        Task PutContractTransferTransaction(ContractTransferTransaction tr);

//        / <summary>
//        / Returns true if transaction completed successfuly
//        / </summary>		
//        Task<bool> CompleteTransaction();
//    }

//    public class ContractTransferTransactionService : IContractTransferTransactionService
//    {
//        private readonly IEthereumQueueOutService _queueOutService;
//        private readonly IEthereumTransactionService _ethereumTransactionService;
//        private readonly ILog _logger;
//        private readonly ICoinContractService _coinContractService;
//        private readonly IBaseSettings _baseSettings;
//        private readonly IQueueExt _queue;

//        public ContractTransferTransactionService(Func<string, IQueueExt> queueFactory,
//            IEthereumQueueOutService queueOutService,
//            IEthereumTransactionService ethereumTransactionService,
//            ILog logger,
//            ICoinContractService coinContractService,
//            IBaseSettings baseSettings)
//        {
//            _queueOutService = queueOutService;
//            _ethereumTransactionService = ethereumTransactionService;
//            _logger = logger;
//            _coinContractService = coinContractService;
//            _baseSettings = baseSettings;
//            _queue = queueFactory(Constants.ContractTransferQueue);
//        }

//        public async Task PutContractTransferTransaction(ContractTransferTransaction tr)
//        {
//            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(tr));
//        }

//        public async Task<bool> CompleteTransaction()
//        {
//            var item = await _queue.GetRawMessageAsync();

//            if (item == null)
//                return false;

//            var contractTransferTr = JsonConvert.DeserializeObject<ContractTransferTransaction>(item.AsString);

//            if (await _ethereumTransactionService.GetTransactionReceipt(contractTransferTr.TransactionHash) == null)
//                return false;

//            if (await _ethereumTransactionService.IsTransactionExecuted(contractTransferTr.TransactionHash, Constants.GasForUserContractTransafer))
//            {
//                await TransferToEthCoinContract(item, contractTransferTr);
//                await _queueOutService.FirePaymentEvent(contractTransferTr.Contract, contractTransferTr.Amount,
//                    contractTransferTr.TransactionHash);
//                await
//                    _logger.WriteInfoAsync("ContractTransferTransactionService", "CompleteTransaction", "",
//                        $"Message sended to ethereum-queue-out : Event from {contractTransferTr.Contract}. Transaction: {contractTransferTr.TransactionHash}");
//            }
//            else
//            {
//                await
//                    _logger.WriteInfoAsync("ContractTransferTransactionService", "CompleteTransaction", "",
//                        $"Transaction failed! : Event from {contractTransferTr.Contract}. Transaction: {contractTransferTr.TransactionHash}");
//            }

//            await _queue.FinishRawMessageAsync(item);
//            return true;
//        }

//        private async Task TransferToEthCoinContract(CloudQueueMessage item, ContractTransferTransaction contractTransferTr)
//        {
//            try
//            {
//                var contractEntity = await _userContractRepository.GetUserContractAsync(contractTransferTr.Contract);

//                var tr = await _coinContractService.CashinOverTransferContract(new Guid(item.Id), _baseSettings.EthCoin, contractEntity.UserWallet, contractTransferTr.Amount);
//                await _logger.WriteInfoAsync("ContractTransferTransactionService", "TransferToEthCoinContract", "",
//                    $"Transfered {contractTransferTr.Amount} Eth from transfer contract to \"{_baseSettings.EthCoin}\" by transaction \"{tr}\". Receiver = {contractEntity.UserWallet}");
//            }
//            catch (Exception e)
//            {
//                await _logger.WriteErrorAsync("ContractTransferTransactionService", "TransferToEthCoinContract",
//                            $"{contractTransferTr.Contract} - {contractTransferTr.TransactionHash}", e);
//            }
//        }
//    }
//}

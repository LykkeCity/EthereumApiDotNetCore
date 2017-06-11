using AzureStorage.Queue;
using Core;
using Core.Exceptions;
using Core.Notifiers;
using Core.Repositories;
using Core.Settings;
using Core.Utils;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Services
{
    public class TransferContractUserAssignment : QueueMessageBase
    {
        public string UserAddress { get; set; }

        public string TransferContractAddress { get; set; }

        public string CoinAdapterAddress { get; set; }
    }

    public interface ITransferContractUserAssignmentQueueService
    {
        Task PushContract(TransferContractUserAssignment assignment);
        Task CompleteTransfer(TransferContractUserAssignment assignment);
    }

    public class TransferContractUserAssignmentQueueService : ITransferContractUserAssignmentQueueService
    {
        private readonly IQueueExt _queue;
        private readonly ITransferContractRepository _transferContractRepository;
        private readonly ISlackNotifier _slackNotifier;
        private readonly ICoinRepository _coinRepository;
        private readonly IQueueFactory _queueFactory;
        private readonly IBaseSettings _settings;
        private readonly Web3 _web3;
        private readonly ITransferContractService _transferContractService;

        public TransferContractUserAssignmentQueueService(Func<string, IQueueExt> queueFactory,
            ITransferContractRepository transferContractRepository, ISlackNotifier slackNotifier,
            ICoinRepository coinRepository, IBaseSettings settings, Web3 web3)
        {
            _web3 = web3;
            _transferContractRepository = transferContractRepository;
            _slackNotifier = slackNotifier;
            _queue = queueFactory(Constants.TransferContractUserAssignmentQueueName);
            _coinRepository = coinRepository;
            _settings = settings;
        }

        public async Task PushContract(TransferContractUserAssignment transferContract)
        {
            string transferContractSerialized = Newtonsoft.Json.JsonConvert.SerializeObject(transferContract);

            await _queue.PutRawMessageAsync(transferContractSerialized);
        }

        public async Task<int> Count()
        {
            return await _queue.Count() ?? 0;
        }

        public async Task CompleteTransfer(TransferContractUserAssignment assignment)
        {
            ICoin coinAdapter = await _coinRepository.GetCoinByAddress(assignment.CoinAdapterAddress);
            if (coinAdapter == null)
            {
                throw new Exception($"CoinAdapterAddress {assignment.CoinAdapterAddress} does not exis");
            }

            string coinAbi;
            if (coinAdapter.ContainsEth)
            {
                coinAbi = _settings.EthAdapterContract.Abi;
            }
            else
            {
                coinAbi = _settings.TokenAdapterContract.Abi;
            }

            var ethereumContract = _web3.Eth.GetContract(coinAbi, assignment.CoinAdapterAddress);
            var function = ethereumContract.GetFunction("setTransferAddressUser");
            //function setTransferAddressUser(address userAddress, address transferAddress) onlyowner{

            string transactionHash =
                await function.SendTransactionAsync(_settings.EthereumMainAccount,
                assignment.UserAddress, assignment.TransferContractAddress);
            var transferContract = await _transferContractRepository.GetAsync(assignment.TransferContractAddress);
            transferContract.AssignmentHash = transactionHash;

            await _transferContractRepository.SaveAsync(transferContract);
        }
    }
}

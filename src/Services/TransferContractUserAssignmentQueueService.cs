using AzureStorage.Queue;
using Core;
using Core.Exceptions;
using Core.Notifiers;
using Core.Repositories;
using Core.Settings;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Services
{
    [DataContract]
    public class TransferContractUserAssignment
    {
        [DataMember]
        public string UserAddress { get; set; }

        [DataMember]
        public string TransferContractAddress { get; set; }

        [DataMember]
        public string CoinAdapterAddress { get; set; }
    }

    public interface ITransferContractUserAssignmentQueueService
    {
        Task PushContract(TransferContractUserAssignment assignment);
        Task<int> Count();
        Task<bool> CompleteTransfer();
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

        public async Task<TransferContractUserAssignment> GetContract()
        {
            string contractSerialized = await GetContractRaw();
            TransferContractUserAssignment contract =
                Newtonsoft.Json.JsonConvert.DeserializeObject<TransferContractUserAssignment>(contractSerialized);

            return contract;
        }

        public async Task<string> GetContractRaw()
        {
            var message = await _queue.GetRawMessageAsync();
            if (message == null)
                NotifyAboutError();

            await _queue.FinishRawMessageAsync(message);

            var contract = message.AsString;

            if (string.IsNullOrWhiteSpace(contract))
                NotifyAboutError();

            return contract;
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

        public void NotifyAboutError()
        {
            _slackNotifier.ErrorAsync("Ethereum integration! User contract pool is empty!");
            throw new BackendException(BackendExceptionType.ContractPoolEmpty, "Transfer contract pool is empty!");
        }

        public async Task<bool> CompleteTransfer()
        {
            var message = await _queue.GetRawMessageAsync();
            if (message == null)
                return false;

            var contract = message.AsString;

            if (string.IsNullOrWhiteSpace(contract))
                return false;

            TransferContractUserAssignment assignment =
                Newtonsoft.Json.JsonConvert.DeserializeObject<TransferContractUserAssignment>(contract);

            ICoin coinAdapter = await _coinRepository.GetCoinByAddress(assignment.CoinAdapterAddress);
            if (coinAdapter == null)
            {
                await _queue.FinishRawMessageAsync(message);
                //log error
                return true;
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
            string transaction =
                await function.SendTransactionAsync(_settings.EthereumMainAccount,
                assignment.UserAddress, assignment.TransferContractAddress);

            await _queue.FinishRawMessageAsync(message);
            return true;
        }
    }
}

using AzureStorage.Queue;
using Core;
using Core.Exceptions;
using Core.Notifiers;
using Core.Repositories;
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
        Task<TransferContractUserAssignment> GetContract();
        Task PushContract(TransferContractUserAssignment assignment);
        Task<int> Count();
    }

    public class TransferContractUserAssignmentQueueService : ITransferContractUserAssignmentQueueService
    {
        private readonly IQueueExt _queue;
        private readonly ITransferContractRepository _transferContractRepository;
        private readonly ISlackNotifier _slackNotifier;
        private readonly ICoinRepository _coinRepository;
        private readonly IQueueFactory _queueFactory;

        public TransferContractUserAssignmentQueueService(Func<string, IQueueExt> queueFactory,
            ITransferContractRepository transferContractRepository, ISlackNotifier slackNotifier,
            ICoinRepository coinRepository)
        {
            _transferContractRepository = transferContractRepository;
            _slackNotifier = slackNotifier;
            _queue = queueFactory(Constants.TransferContractUserAssignmentQueueName);
            _coinRepository = coinRepository;
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
    }
}

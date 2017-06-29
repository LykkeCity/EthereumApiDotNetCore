using AzureStorage.Queue;
using Core;
using Core.Exceptions;
using Core.Notifiers;
using Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public interface ITransferContractQueueService
    {
        Task<ITransferContract> GetContract();
        Task PushContract(ITransferContract transferContract);
        Task<int> Count();
    }

    public class TransferContractQueueService : ITransferContractQueueService
    {
        private readonly IQueueExt _queue;
        private readonly ITransferContractRepository _transferContractRepository;
        private readonly ISlackNotifier _slackNotifier;
        private readonly ICoinRepository _coinRepository;

        public TransferContractQueueService(IQueueExt queue,
            ITransferContractRepository transferContractRepository, ISlackNotifier slackNotifier,
            ICoinRepository coinRepository)
        {
            _transferContractRepository = transferContractRepository;
            _slackNotifier = slackNotifier;
            _queue = queue;
            _coinRepository = coinRepository;
        }

        public async Task<ITransferContract> GetContract()
        {
            string contractSerialized = await GetContractRaw();
            ITransferContract contract = Newtonsoft.Json.JsonConvert.DeserializeObject<TransferContract>(contractSerialized);

            return contract;
        }

        public async Task<string> GetContractRaw()
        {
            //TODO: think about locking code below
            var message = await _queue.GetRawMessageAsync();
            if (message == null)
                NotifyAboutError();

            await _queue.FinishRawMessageAsync(message);

            var contract = message.AsString;

            if (string.IsNullOrWhiteSpace(contract))
                NotifyAboutError();

            return contract;
        }

        public async Task PushContract(ITransferContract transferContract)
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
            _slackNotifier.ErrorAsync("Ethereum Core Service! User contract pool is empty!");
            throw new ClientSideException(ExceptionType.ContractPoolEmpty, "Transfer contract pool is empty!");
        }
    }
}

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
    public interface ITransferContractQueueServiceFactory
    {
        ITransferContractQueueService Get(string queueName);
    }

    public class TransferContractQueueServiceFactory : ITransferContractQueueServiceFactory
    {
        private readonly ICoinRepository _coinRepository;
        private readonly IQueueFactory _queueFactory;
        private readonly ISlackNotifier _slackNotifier;
        private readonly ITransferContractRepository _transferContractRepository;

        public TransferContractQueueServiceFactory(IQueueFactory queueFactory,
            ITransferContractRepository transferContractRepository, ISlackNotifier slackNotifier,
            ICoinRepository coinRepository)
        {
            _queueFactory = queueFactory;
            _transferContractRepository = transferContractRepository;
            _slackNotifier = slackNotifier;
            _coinRepository = coinRepository;
        }

        public ITransferContractQueueService Get(string queueName)
        {
            IQueueExt queue = _queueFactory.Build(queueName);

            return new TransferContractQueueService(queue, _transferContractRepository, _slackNotifier, _coinRepository);
        }
    }
}

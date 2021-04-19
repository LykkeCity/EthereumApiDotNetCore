using AzureStorage.Queue;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Notifiers;
using Lykke.Service.EthereumCore.Core.Repositories;

namespace Lykke.Service.EthereumCore.Services
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

using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Notifiers;

namespace Lykke.Service.EthereumCore.Services
{
    public class Erc20DepositContractQueueServiceFactory : IErc20DepositContractQueueServiceFactory
    {
        private readonly IQueueFactory _queueFactory;
        private readonly ISlackNotifier _slackNotifier;


        public Erc20DepositContractQueueServiceFactory(
            IQueueFactory queueFactory,
            ISlackNotifier slackNotifier)
        {
            _queueFactory = queueFactory;
            _slackNotifier = slackNotifier;
        }


        public IErc20DepositContractQueueService Get(string queueName)
        {
            return new Erc20DepositContractQueueService
            (
                _queueFactory.Build(queueName),
                _slackNotifier
            );
        }
    }
}
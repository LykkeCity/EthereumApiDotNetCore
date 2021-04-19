using AzureStorage.Queue;

namespace Lykke.Service.EthereumCore.Core
{
    public interface IQueueFactory
    {
        IQueueExt Build(string queueName = "default-queue-name");
    }
}

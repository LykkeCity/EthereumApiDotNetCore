using AzureStorage.Queue;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.SettingsReader;

namespace Lykke.Service.EthereumCore.AzureRepositories
{
    public class QueueFactory : IQueueFactory
    {
        private readonly IReloadingManager<BaseSettings> _settings;

        public QueueFactory(IReloadingManager<BaseSettings> settings)
        {
            _settings = settings;
        }

        public IQueueExt Build(string queueName = "default-queue-name")
        {
            return AzureQueueExt.Create(_settings.ConnectionString(x => x.Db.DataConnString), Constants.StoragePrefix + queueName);
        }
    }
}

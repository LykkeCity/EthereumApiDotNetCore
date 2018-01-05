using AzureStorage.Queue;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.SettingsReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.AzureRepositories
{
    public class QueueFactory : IQueueFactory
    {
        private readonly IReloadingManager<IBaseSettings> _settings;

        public QueueFactory(IReloadingManager<IBaseSettings> settings)
        {
            _settings = settings;
        }

        public IQueueExt Build(string queueName = "default-queue-name")
        {
            return AzureQueueExt.Create(_settings.Nested(x => x.Db.DataConnString), Constants.StoragePrefix + queueName);
        }
    }
}

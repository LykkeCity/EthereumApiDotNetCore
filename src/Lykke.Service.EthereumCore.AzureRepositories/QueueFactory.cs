using AzureStorage.Queue;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.AzureRepositories
{
    public class QueueFactory : IQueueFactory
    {
        private readonly IBaseSettings _settings;

        public QueueFactory(IBaseSettings settings)
        {
            _settings = settings;
        }

        public IQueueExt Build(string queueName = "default-queue-name")
        {
            return new AzureQueueExt(_settings.Db.DataConnString, Constants.StoragePrefix + queueName);
        }
    }
}

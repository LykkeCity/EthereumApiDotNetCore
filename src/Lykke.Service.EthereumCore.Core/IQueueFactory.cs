using AzureStorage.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core
{
    public interface IQueueFactory
    {
        IQueueExt Build(string queueName = "default-queue-name");
    }
}

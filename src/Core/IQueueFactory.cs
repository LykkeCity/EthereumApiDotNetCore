using AzureStorage.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core
{
    public interface IQueueFactory
    {
        IQueueExt Build(string queueName = "default-queue-name");
    }
}

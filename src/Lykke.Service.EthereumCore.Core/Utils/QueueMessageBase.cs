using System;

namespace Lykke.Service.EthereumCore.Core.Utils
{
    public class QueueMessageBase
    {
        public int DequeueCount { get; set; }
        public string LastError { get; set; }
        public DateTime PutDateTime { get; set; }
    }
}

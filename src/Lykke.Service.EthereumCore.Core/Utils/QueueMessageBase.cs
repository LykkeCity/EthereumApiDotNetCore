using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.EthereumCore.Core.Utils
{
    public class QueueMessageBase
    {
        public int DequeueCount { get; set; }
        public string LastError { get; set; }
        public DateTime PutDateTime { get; set; }
    }
}

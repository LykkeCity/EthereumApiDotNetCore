using Lykke.Service.EthereumCore.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.EthereumCore.Services.New.Models
{
    public class CoinEventCashinCompletedMessage : QueueMessageBase
    {
        public string TransactionHash { get; set; }
    }
}

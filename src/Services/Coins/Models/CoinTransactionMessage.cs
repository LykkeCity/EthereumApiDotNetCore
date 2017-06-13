using Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Coins.Models
{
    public class CoinTransactionMessage : QueueMessageBase
    {
        public string TransactionHash { get; set; }
        public string OperationId { get; set; }
    }
}

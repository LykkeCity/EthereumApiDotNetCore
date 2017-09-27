using Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services.New.Models
{
    public class CoinEventCashinCompletedMessage : QueueMessageBase
    {
        public string TransactionHash { get; set; }
    }
}

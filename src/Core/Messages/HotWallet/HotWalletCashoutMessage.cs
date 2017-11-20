using Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Messages.HotWallet
{
    public class HotWalletCashoutMessage : QueueMessageBase
    {
        public string OperationId { get; set; }
    }
}

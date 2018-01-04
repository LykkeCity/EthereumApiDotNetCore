using Lykke.Service.EthereumCore.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.EthereumCore.Core.Messages.HotWallet
{
    public class HotWalletCashoutMessage : QueueMessageBase
    {
        public string OperationId { get; set; }
    }
}

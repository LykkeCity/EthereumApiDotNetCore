using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.EthereumCore.Core.Utils;

namespace Lykke.Service.EthereumCore.Core.Messages.LykkePay
{
    public class LykkePayErc20TransferMessage : QueueMessageBase
    {
        public string OperationId { get; set; }
    }
}

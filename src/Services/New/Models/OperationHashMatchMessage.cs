using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.EthereumCore.Services.New.Models
{
    public class OperationHashMatchMessage : QueueMessageBase, IOperationToHashMatch
    {
        public string OperationId { get; set; }
        public string TransactionHash { get; set; }
    }
}

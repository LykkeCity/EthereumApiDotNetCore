using Lykke.Service.EthereumCore.Core.Utils;

namespace Lykke.Service.EthereumCore.Services.Coins.Models
{
    public class CoinTransactionMessage : QueueMessageBase
    {
        public string TransactionHash { get; set; }
        public string OperationId { get; set; }
    }
}

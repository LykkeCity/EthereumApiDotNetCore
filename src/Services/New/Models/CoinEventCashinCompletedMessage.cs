using Lykke.Service.EthereumCore.Core.Utils;

namespace Lykke.Service.EthereumCore.Services.New.Models
{
    public class CoinEventCashinCompletedMessage : QueueMessageBase
    {
        public string TransactionHash { get; set; }
    }
}

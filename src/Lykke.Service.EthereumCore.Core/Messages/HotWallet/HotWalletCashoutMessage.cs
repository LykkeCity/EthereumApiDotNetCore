using Lykke.Service.EthereumCore.Core.Utils;

namespace Lykke.Service.EthereumCore.Core.Messages.HotWallet
{
    public class HotWalletCashoutMessage : QueueMessageBase
    {
        public string OperationId { get; set; }
    }
}

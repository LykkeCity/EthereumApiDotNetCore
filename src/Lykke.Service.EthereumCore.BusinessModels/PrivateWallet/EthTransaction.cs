using System.Numerics;

namespace Lykke.Service.EthereumCore.BusinessModels.PrivateWallet
{
    public class EthTransaction : TransactionBase
    {
        public override BigInteger Value { get; set; }
    }
}

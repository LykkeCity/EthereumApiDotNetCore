using System.Numerics;

namespace Lykke.Service.EthereumCore.BusinessModels.PrivateWallet
{
    public abstract class TransactionBase
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public BigInteger GasAmount { get; set; }
        public BigInteger GasPrice { get; set; }
        public virtual BigInteger Value { get; set; }
    }
}

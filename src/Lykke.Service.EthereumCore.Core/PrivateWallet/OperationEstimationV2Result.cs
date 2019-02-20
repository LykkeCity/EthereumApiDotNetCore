using System.Numerics;

namespace Lykke.Service.EthereumCore.Core.PrivateWallet
{
    public class OperationEstimationV2Result
    {
        public BigInteger GasAmount { get; set; }

        public BigInteger GasPrice { get; set; }

        public BigInteger EthAmount { get; set; }

        public bool IsAllowed { get; set; }
    }
}

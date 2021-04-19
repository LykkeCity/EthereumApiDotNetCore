using System.Numerics;

namespace Lykke.Service.EthereumCore.Services.Model
{
    public class OperationEstimationResult
    {
        public bool IsAllowed { get; set; }
        public BigInteger GasAmount { get; set; }
    }
}

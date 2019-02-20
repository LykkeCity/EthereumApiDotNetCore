using System.Numerics;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.PrivateWallet
{
    public interface IEstimationService
    {
        Task<OperationEstimationV2Result> EstimateTransactionExecutionCostAsync(
            string fromAddress,
            string toAddress,
            BigInteger amount,
            BigInteger gasPrice,
            string functionCallData);
    }
}

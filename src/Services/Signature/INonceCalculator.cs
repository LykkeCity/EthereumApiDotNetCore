using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;

namespace Lykke.Service.EthereumCore.Services.Signature
{
    public interface INonceCalculator
    {
        Task<HexBigInteger> GetNonceAsync(string fromAddress, bool checkTxPool);

        Task<HexBigInteger> GetNonceLatestAsync(string fromAddress);
    }
}
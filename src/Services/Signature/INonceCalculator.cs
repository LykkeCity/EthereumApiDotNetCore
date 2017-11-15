using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;

namespace Services.Signature
{
    public interface INonceCalculator
    {
        Task<HexBigInteger> GetNonceAsync(string fromAddress, bool checkTxPool);
    }
}
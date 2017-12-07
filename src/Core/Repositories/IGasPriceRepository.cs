using System.Numerics;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IGasPrice
    {
        BigInteger Max { get; set; }

        BigInteger Min { get; set; }
    }

    public class GasPrice : IGasPrice
    {
        public BigInteger Max { get; set; }
        public BigInteger Min { get; set; }
    }

    public interface IGasPriceRepository
    {
        Task<IGasPrice> GetAsync();

        Task SetAsync(IGasPrice gasPrice);
    }
}
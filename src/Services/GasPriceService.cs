using System.Numerics;
using System.Threading.Tasks;
using Core.Repositories;

namespace Services
{
    public interface IGasPriceService
    {
        Task<(BigInteger Min, BigInteger Max)> GetAsync();

        Task SetAsync(BigInteger min, BigInteger max);
    }

    public class GasPriceService : IGasPriceService
    {
        private readonly IGasPriceRepository _repository;


        public GasPriceService(
            IGasPriceRepository repository)
        {
            _repository = repository;
        }

        public async Task<(BigInteger Min, BigInteger Max)> GetAsync()
        {
            var gasPrice = await _repository.GetAsync();

            return (gasPrice.Min, gasPrice.Max);
        }

        public async Task SetAsync(BigInteger min, BigInteger max)
        {
            await _repository.SetAsync(new GasPrice
            {
                Max = max,
                Min = min
            });
        }
    }
}
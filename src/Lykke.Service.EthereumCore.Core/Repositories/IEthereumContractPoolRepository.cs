using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface IEthereumContractPool
    {
        string TxHashes { get; set; }
    }

    public class EthereumContractPool : IEthereumContractPool
    {
        public string TxHashes { get; set; }
    }

    public interface IEthereumContractPoolRepository
    {
        Task SaveAsync(IEthereumContractPool pool);

        Task ClearAsync();
        Task<IEthereumContractPool> GetAsync();

        Task<bool> GetOrDefaultAsync(string contractAddress);

        Task InsertOrReplaceAsync(string contractAddress);
    }
}

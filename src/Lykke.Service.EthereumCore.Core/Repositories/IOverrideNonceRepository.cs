using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface IOverrideNonceRepository
    {
        Task AddAsync(string address, string nonce);
        Task<Dictionary<string, string>> GetAllAsync();
        Task<string> GetNonceAsync(string address);
        Task RemoveAsync(string address);
    }
}

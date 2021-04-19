using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Exceptions;

namespace Lykke.Service.EthereumCore.Core.PassToken
{
    public interface IBlockPassService
    {
        /// <summary>
        /// Add contract address to BlockPass white list
        /// </summary>
        /// <param name="address">eth address</param>
        /// <returns>ticketId</returns>
        /// <exception cref="ClientSideException">Throws exception on unexpected response from pass API</exception>
        Task<string> AddToWhiteListAsync(string address);
    }
}

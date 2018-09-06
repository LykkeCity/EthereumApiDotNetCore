using Lykke.Service.EthereumCore.PassTokenIntegration.Models.Requests;
using Refit;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.PassTokenIntegration.Exceptions;

namespace Lykke.Service.EthereumCore.PassTokenIntegration
{
    public interface IBlockPassClient
    {
        /// <summary>
        ///  Whitelist Address
        /// </summary>
        /// <exception cref="NotOkException">Throws in the case of 4xx or 5xx http status code</exception>
        [Post("/api/whitelist/v2/ticket")]
        Task<EthAddressResponse> WhitelistAddressAsync(EthAddressRequest addressModel);
    }
}

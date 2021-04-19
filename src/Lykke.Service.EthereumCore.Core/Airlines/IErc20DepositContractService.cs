using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Common;
using Lykke.Service.EthereumCore.Core.Repositories;

namespace Lykke.Service.EthereumCore.Core.Airlines
{
    public interface IAirlinesErc20DepositContractService : IErc20DepositContractLocatorService
    {
        Task<string> AssignContractAsync(string userAddress);

        Task<string> CreateContractAsync();

        Task<IEnumerable<string>> GetContractAddressesAsync(IEnumerable<string> txHashes);

        Task<string> GetContractAddressAsync(string userAddress);

        Task<string> GetUserAddressAsync(string contractUser);

        Task ProcessAllAsync(Func<IErc20DepositContract, Task> processAction);

        Task<string> RecievePaymentFromDepositContractAsync(string depositContractAddress,
            string erc20TokenAddress, string destinationAddress, string tokenAmount);
    }
}

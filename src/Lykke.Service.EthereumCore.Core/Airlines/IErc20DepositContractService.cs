using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Common;
using Lykke.Service.EthereumCore.Core.Repositories;

namespace Lykke.Service.EthereumCore.Core.Airlines
{
    public interface IAirlinesErc20DepositContractService : IErc20DepositContractLocatorService
    {
        Task<string> AssignContract(string userAddress);

        Task<string> CreateContract();

        Task<IEnumerable<string>> GetContractAddresses(IEnumerable<string> txHashes);

        Task<string> GetContractAddress(string userAddress);

        Task<string> GetUserAddress(string contractUser);

        Task ProcessAllAsync(Func<IErc20DepositContract, Task> processAction);

        Task<string> RecievePaymentFromDepositContract(string depositContractAddress,
            string erc20TokenAddress, string destinationAddress, string tokenAmount);
    }
}

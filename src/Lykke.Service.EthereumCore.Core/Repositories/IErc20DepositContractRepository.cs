using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface IErc20DepositContract
    {
        string UserAddress { get; set; }

        string ContractAddress { get; set; }
    }

    public class Erc20DepositContract : IErc20DepositContract
    {
        public string UserAddress { get; set; }
        public string ContractAddress { get; set; }
    }

    [Obsolete]
    public interface IErc20DepositContractRepositoryOld
    {
        Task AddOrReplace(IErc20DepositContract depositContract);

        Task<bool> Contains(string contractAddress);

        Task<IErc20DepositContract> Get(string userAddress);

        Task<IEnumerable<IErc20DepositContract>> GetAll();

        Task ProcessAllAsync(Func<IErc20DepositContract, Task> processAction);

        Task<IErc20DepositContract> GetByContractAddress(string contractAddress);
    }

    public interface IErc223DepositContractRepository : IErc20DepositContractRepositoryOld
    {
        Task<(IEnumerable<IErc20DepositContract>, string)> GetByTokenAsync(int take, string continuationToken);
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface ITransferContract
    {
        string ContractAddress { get; set; }
        string UserAddress { get; set; }
        string CoinAdapterAddress { get; set; }
        string ExternalTokenAddress { get; set; }
        bool ContainsEth { get; set; }
        string AssignmentHash { get; set; }
    }

    public class TransferContract : ITransferContract
    {
        public string ContractAddress { get; set; }
        public string UserAddress { get; set; }
        public string CoinAdapterAddress { get; set; }
        public string ExternalTokenAddress { get; set; }
        public bool ContainsEth { get; set; }
        public string AssignmentHash { get; set; }
    }

    public interface ITransferContractRepository
    {
        Task SaveAsync(ITransferContract transferContract);
        Task<ITransferContract> GetAsync(string transferContractAddress);
        Task ProcessAllAsync(Func<ITransferContract, Task> processAction);
        Task<ITransferContract> GetAsync(string userAddress, string coinAdapterAddress);
        Task<(IEnumerable<ITransferContract>, string)> GetByTokenAsync(int take, string continuationToken);
    }
}

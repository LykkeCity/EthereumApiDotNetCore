using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface IBlackListAddress
    {
        string Address { get; set; }
    }

    public class BlackListAddress : IBlackListAddress
    {
        public string Address { get; set; }
    }

    public interface IBlackListAddressesRepository
    {
        Task SaveAsync(IBlackListAddress address);
        Task<IBlackListAddress> GetAsync(string address);
        Task DeleteAsync(string address);
    }
}

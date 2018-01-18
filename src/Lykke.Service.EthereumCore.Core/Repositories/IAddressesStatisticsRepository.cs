using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface IAddressStatistics
    {
        string Address { get; set; }
        string TotalCashouts { get; set; }
        string FailedCashouts { get; set; }
    }

    public class AddressStatistics : IAddressStatistics
    {
        public string Address { get; set; }
        public string TotalCashouts { get; set; }
        public string FailedCashouts { get; set; }
    }

    public interface IAddressStatisticsRepository
    {
        Task SaveAsync(IAddressStatistics address);
        Task<IAddressStatistics> GetAsync(string address);
        Task DeleteAsync(string address);
    }
}

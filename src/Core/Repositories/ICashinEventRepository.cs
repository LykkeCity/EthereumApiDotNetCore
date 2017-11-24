using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Core.Repositories
{

    public interface ICashinEvent
    {
        string CoinAdapterAddress {get;set;}
        string TransactionHash { get; set; }
        string UserAddress { get; set; }
        string ContractAddress { get; set; }
        string Amount { get; set; }
    }

    public class CashinEvent : ICashinEvent
    {
        public string CoinAdapterAddress { get; set; }
        public string TransactionHash { get; set; }
        public string UserAddress { get; set; }
        public string ContractAddress { get; set; }
        public string Amount { get; set; }
    }

    public interface ICashinEventRepository
    {
        Task<ICashinEvent> GetAsync(string transactionHash);
        Task InsertAsync(ICashinEvent cashinEvent);
        Task SyncedToAsync(BigInteger to);
    }
}

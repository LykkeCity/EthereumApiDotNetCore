using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IPendingTransaction
    {
        string CoinAdapterAddress { get; set; }
        string UserAddress { get; set; }
        string TransactionHash { get; set; }
    }

    public class PendingTransaction : IPendingTransaction
    {
        public string CoinAdapterAddress { get; set; }
        public string UserAddress { get; set; }
        public string TransactionHash { get; set; }
    }

    public interface IPendingTransactionsRepository
    {
        Task<IPendingTransaction> GetAsync(string coinAdapterAddress, string userAddress);
        Task InsertOrReplace(IPendingTransaction pendingTransactions);
        Task Delete(string transactionHash);
    }
}

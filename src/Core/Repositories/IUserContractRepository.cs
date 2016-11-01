using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IUserContract
    {
        string Address { get; }
        string UserWallet { get; }
        DateTime CreateDt { get; }
        int BalanceNotChangedCount { get; set; }

        decimal LastBalance { get; set; }
    }

    public class UserContract : IUserContract
    {
        public string Address { get; set; }
        public string UserWallet { get; set; }
        public DateTime CreateDt { get; set; }
        public int BalanceNotChangedCount { get; set; }
        public decimal LastBalance { get; set; }
    }

    public interface IUserContractRepository
    {
        Task AddAsync(IUserContract contract);
        Task ProcessContractsAsync(Func<IEnumerable<IUserContract>, Task> chunks);
        Task ReplaceAsync(IUserContract contract);
        Task<IUserContract> GetUserContractAsync(string address);
        void DeleteTable();
        Task UpdateUserWalletAsync(UserContract userContract);
    }
}

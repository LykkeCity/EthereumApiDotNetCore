using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IUserPaymentHistory
    {
        string AdapterAddress { get; set; }
        string TransactionHash { get; set; }
        string UserAddress { get; set; }
        string ContractAddress { get; set; }
        string Amount { get; set; }
        DateTime CreatedDate { get; set; }
        string Note { get; set; }
    }

    public class UserPaymentHistory : IUserPaymentHistory
    {
        public string TransactionHash { get; set; }
        public string ContractAddress { get; set; }
        public string UserAddress { get; set; }
        public string Amount { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Note { get; set; }
        public string AdapterAddress { get; set; }
    }

    public interface IUserPaymentHistoryRepository
    {
        Task SaveAsync(IUserPaymentHistory transferContract);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IUserPayment
    {
        string UserAddress { get; set; }
        string ContractAddress { get; set; }
        string Amount { get; set; }
        DateTime CreatedDate { get; set; }
    }

    public class UserPayment : IUserPayment
    {
        public string ContractAddress { get; set; }
        public string UserAddress { get; set; }
        public string Amount { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public interface IUserPaymentRepository
    {
        Task SaveAsync(IUserPayment transferContract);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IHotWalletTransaction
    {
        string OperationId { get; set; }
        string TransactionHash { get; set; }
    }

    public class HotWalletCashoutTransaction : IHotWalletTransaction
    {
        public string OperationId { get; set; }
        public string TransactionHash { get; set; }
    }

    public interface IHotWalletTransactionRepository
    {
        Task SaveAsync(IHotWalletTransaction cashoutTransaction);
        Task<IHotWalletTransaction> GetByOperationIdAsync(string operationId);
        Task<IHotWalletTransaction> GetByTransactionHashAsync(string transactionHash);
    }
}

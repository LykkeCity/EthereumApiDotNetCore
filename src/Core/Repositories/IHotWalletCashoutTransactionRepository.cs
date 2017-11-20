using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IHotWalletCashoutTransaction
    {
        string OperationId { get; set; }
        string TransactionHash { get; set; }
    }

    public class HotWalletCashoutTransaction : IHotWalletCashoutTransaction
    {
        public string OperationId { get; set; }
        public string TransactionHash { get; set; }
    }

    public interface IHotWalletCashoutTransactionRepository
    {
        Task SaveAsync(IHotWalletCashoutTransaction cashoutTransaction);
        Task<IHotWalletCashoutTransaction> GetByOperationIdAsync(string operationId);
        Task<IHotWalletCashoutTransaction> GetByTransactionHashAsync(string transactionHash);
    }
}

using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface ICoinTransaction
    {
        string TransactionHash { get; }

        int ConfirmationLevel { get; }

        bool Error { get; set; }
    }

    public class CoinTransaction : ICoinTransaction
    {
        public string TransactionHash { get; set; }
        public int ConfirmationLevel { get; set; }
        public bool Error { get; set; }
    }


    public interface ICoinTransactionRepository
    {
        Task AddAsync(ICoinTransaction transaction);
        Task InsertOrReplaceAsync(ICoinTransaction transaction);
        Task<ICoinTransaction> GetTransaction(string transactionHash);
        void DeleteTable();
    }
}

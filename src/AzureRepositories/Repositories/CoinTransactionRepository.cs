using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{

    public class CoinTransactionEntity : TableEntity, ICoinTransaction
    {
        public static string GeneratePartitionKey()
        {
            return "Transactions";
        }
        public string TransactionHash => RowKey;
        public int ConfirmationLevel { get; set; }
        public bool Error { get; set; }

        public static CoinTransactionEntity Create(ICoinTransaction transaction)
        {
            return new CoinTransactionEntity
            {
                RowKey = transaction.TransactionHash,
                PartitionKey = GeneratePartitionKey(),
                ConfirmationLevel = transaction.ConfirmationLevel,
                Error = transaction.Error
            };
        }

    }


    public class CoinTransactionRepository : ICoinTransactionRepository
    {
        private readonly INoSQLTableStorage<CoinTransactionEntity> _table;

        public CoinTransactionRepository(INoSQLTableStorage<CoinTransactionEntity> table)
        {
            _table = table;
        }

        public async Task AddAsync(ICoinTransaction transaction)
        {
            var entity = CoinTransactionEntity.Create(transaction);
            await _table.InsertAsync(entity);
        }

        public async Task InsertOrReplaceAsync(ICoinTransaction transaction)
        {
            var entity = CoinTransactionEntity.Create(transaction);

            await _table.InsertOrReplaceAsync(entity);
        }

        public async Task<ICoinTransaction> GetTransaction(string transactionHash)
        {
            return await _table.GetDataAsync(CoinTransactionEntity.GeneratePartitionKey(), transactionHash);
        }

        public void DeleteTable()
        {
        }
    }
}

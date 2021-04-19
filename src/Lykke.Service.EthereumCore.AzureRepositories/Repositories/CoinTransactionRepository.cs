using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

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
}

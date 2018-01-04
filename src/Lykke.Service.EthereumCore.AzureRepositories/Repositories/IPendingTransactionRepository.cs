using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class PendingTransactionEntity : TableEntity, IPendingTransaction
    {
        public string CoinAdapterAddress { get; set; }
        public string UserAddress
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }
        public string TransactionHash { get; set; }

        public static string GeneratePartitionKey(string coinAdapter)
        {
            return $"PendingTransaction_{coinAdapter}";
        }
        public static PendingTransactionEntity CreateEntity(IPendingTransaction pendingTransaction)
        {
            return new PendingTransactionEntity
            {
                PartitionKey = GeneratePartitionKey(pendingTransaction.CoinAdapterAddress),
                CoinAdapterAddress = pendingTransaction.CoinAdapterAddress,
                UserAddress = pendingTransaction.UserAddress,
                TransactionHash = pendingTransaction.TransactionHash
            };
        }
    }

    public class PendingTransactionsRepository : IPendingTransactionsRepository
    {
        private readonly INoSQLTableStorage<PendingTransactionEntity> _table;
        private const string _indexName = "HashIndex";
        private readonly INoSQLTableStorage<AzureIndex> _index;

        public PendingTransactionsRepository(INoSQLTableStorage<PendingTransactionEntity> table, INoSQLTableStorage<AzureIndex> index)
        {
            _table = table;
            _index = index;
        }

        public async Task Delete(string transactionHash)
        {
            AzureIndex index = await _index.GetDataAsync(_indexName, transactionHash);
            if (index == null)
            {
                return;
            }

            IPendingTransaction transaction = await _table.GetDataAsync(index);

            if (transaction == null)
            {
                return;
            }
            await _table.DeleteIfExistAsync(PendingTransactionEntity.GeneratePartitionKey(transaction.CoinAdapterAddress), transaction.UserAddress);
            await _table.DeleteIfExistAsync(_indexName, index.RowKey);
        }

        public async Task<IPendingTransaction> GetAsync(string coinAdapterAddress, string userAddress)
        {
            IPendingTransaction transaction = await _table.GetDataAsync(PendingTransactionEntity.GeneratePartitionKey(coinAdapterAddress), userAddress);

            return transaction;
        }

        public async Task InsertOrReplace(IPendingTransaction pendingTransactions)
        {
            var entity = PendingTransactionEntity.CreateEntity(pendingTransactions);
            AzureIndex index = new AzureIndex(_indexName, pendingTransactions.TransactionHash, entity);
            await _table.InsertOrReplaceAsync(entity);
            await _index.InsertOrReplaceAsync(index);
        }
    }
}

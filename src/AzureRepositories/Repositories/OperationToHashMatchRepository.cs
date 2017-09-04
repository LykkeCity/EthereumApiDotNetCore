using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using System.Numerics;

namespace AzureRepositories.Repositories
{
    public class OperationToHashMatchHistoryEntity : TableEntity, IOperationToHashMatch
    {
        public static string GetPartitionKey(string operationId)
        {
            return operationId;
        }

        public string TransactionHash
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        public string OperationId
        {
            get;
            set;
        }

        public static OperationToHashMatchHistoryEntity Create(IOperationToHashMatch match)
        {
            return new OperationToHashMatchHistoryEntity
            {
                PartitionKey = GetPartitionKey(match.OperationId),
                OperationId = match.OperationId,
                TransactionHash = match.TransactionHash
            };
        }
    }

    public class HashToOperationMatchEntity : TableEntity, IOperationToHashMatch
    {
        public static string GetPartitionKey()
        {
            return $"OperationToHashMatch_Hash";
        }

        public string TransactionHash
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        public string OperationId
        {
            get;
            set;
        }

        public static HashToOperationMatchEntity Create(IOperationToHashMatch match)
        {
            return new HashToOperationMatchEntity
            {
                PartitionKey = GetPartitionKey(),
                OperationId = match.OperationId,
                TransactionHash = match.TransactionHash
            };
        }
    }

    public class OperationToHashMatchEntity : TableEntity, IOperationToHashMatch
    {
        public static string GetPartitionKey()
        {
            return $"OperationToHashMatch_Id";
        }

        public string TransactionHash
        {
            get;
            set;
        }

        public string OperationId
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        public static OperationToHashMatchEntity Create(IOperationToHashMatch match)
        {
            return new OperationToHashMatchEntity
            {
                PartitionKey = GetPartitionKey(),
                OperationId = match.OperationId,
                TransactionHash = match.TransactionHash
            };
        }
    }

    public class OperationToHashMatchRepository : IOperationToHashMatchRepository
    {
        private readonly INoSQLTableStorage<OperationToHashMatchEntity> _table;
        private readonly INoSQLTableStorage<HashToOperationMatchEntity> _tableReverse;
        private readonly INoSQLTableStorage<OperationToHashMatchHistoryEntity> _tableHistory;

        public OperationToHashMatchRepository(INoSQLTableStorage<OperationToHashMatchEntity> table, 
            INoSQLTableStorage<HashToOperationMatchEntity> table2,
            INoSQLTableStorage<OperationToHashMatchHistoryEntity> tableHistory)
        {
            _table = table;
            _tableReverse = table2;
            _tableHistory = tableHistory;
        }

        public async Task<IOperationToHashMatch> GetAsync(string operationId)
        {
            var match = await _table.GetDataAsync(OperationToHashMatchEntity.GetPartitionKey(), operationId);

            return match;
        }

        public async Task<IOperationToHashMatch> GetByHashAsync(string transactionHash)
        {
           var entity = await  _tableReverse.GetDataAsync(HashToOperationMatchEntity.GetPartitionKey(), transactionHash);

            return entity;
        }

        public async Task<IEnumerable<IOperationToHashMatch>> GetHistoricalForOperationAsync(string operationId)
        {
            var entities = await _tableHistory.GetDataAsync(operationId);

            return entities;
        }

        public async Task InsertOrReplaceHistoricalAsync(IOperationToHashMatch match)
        {
            var historyEntity = OperationToHashMatchHistoryEntity.Create(match);

            await _tableHistory.InsertOrReplaceAsync(historyEntity);
        }

        public async Task InsertOrReplaceAsync(IOperationToHashMatch match)
        {
            var entity = OperationToHashMatchEntity.Create(match);
            var historyEntity = OperationToHashMatchHistoryEntity.Create(match);

            if (match.TransactionHash != null)
            {
                var entityReverse = HashToOperationMatchEntity.Create(match);
                await _tableReverse.InsertOrReplaceAsync(entityReverse);
            }
            else
            {
                await _tableReverse.DeleteIfExistAsync(HashToOperationMatchEntity.GetPartitionKey(), match.TransactionHash);
            }

            await _table.InsertOrReplaceAsync(entity);
            await _tableHistory.InsertOrReplaceAsync(historyEntity);
        }

        public async Task ProcessAllAsync(Func<IEnumerable<IOperationToHashMatch>, Task> processAction)
        {
            Action<IEnumerable<IOperationToHashMatch>> function = async (items) =>
            {
                await processAction(items);
            };

            await _table.GetDataByChunksAsync(OperationToHashMatchEntity.GetPartitionKey(), function);
        }

        public async Task ProcessHistoricalAsync(string operationId, Func<IEnumerable<IOperationToHashMatch>, Task> processAction)
        {
            Action<IEnumerable<IOperationToHashMatch>> function = async (items) =>
            {
                await processAction(items);
            };

            await _tableHistory.GetDataByChunksAsync(operationId, function);
        }
    }
}

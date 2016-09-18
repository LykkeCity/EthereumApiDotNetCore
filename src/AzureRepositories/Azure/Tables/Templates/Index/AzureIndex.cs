using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Azure.Tables.Templates.Index
{
    public interface IAzureIndex
    {
        string PrimaryPartitionKey { get; }
        string PrimaryRowKey { get; }
    }

    public class AzureIndex : TableEntity, IAzureIndex
    {
        public AzureIndex()
        {

        }

        public AzureIndex(string partitionKey, string rowKey, string primaryPartitionKey, string primaryRowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
            PrimaryPartitionKey = primaryPartitionKey;
            PrimaryRowKey = primaryRowKey;
        }

        public AzureIndex(string partitionKey, string rowKey, ITableEntity tableEntity)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
            PrimaryPartitionKey = tableEntity.PartitionKey;
            PrimaryRowKey = tableEntity.RowKey;

        }



        public string PrimaryPartitionKey { get; set; }
        public string PrimaryRowKey { get; set; }


        public static AzureIndex Create(string partitionKey, string rowKey, ITableEntity tableEntity)
        {
            return new AzureIndex
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                PrimaryPartitionKey = tableEntity.PartitionKey,
                PrimaryRowKey = tableEntity.RowKey
            };
        }

        public static AzureIndex Create(string partitionKey, string rowKey, string primaryPartitionKey, string primaryRowKey)
        {
            return new AzureIndex
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                PrimaryPartitionKey = primaryPartitionKey,
                PrimaryRowKey = primaryRowKey
            };
        }
    }

    public static class AzureIndexUtils
    {
        public static Task<T> DeleteAsync<T>(this INoSQLTableStorage<T> tableStorage, IAzureIndex index) where T : ITableEntity, new()
        {
            return tableStorage.DeleteAsync(index.PrimaryPartitionKey, index.PrimaryRowKey);
        }

        public static async Task<IEnumerable<T>> GetDataAsync<T>(this INoSQLTableStorage<T> tableStorage,
                 IEnumerable<IAzureIndex> indices, int pieces = 15, Func<T, bool> filter = null) where T : ITableEntity, new()
        {
            var idx = indices.ToArray();
            if (idx.Length == 0)
                return new T[0];

            var partitionKey = idx.First().PrimaryPartitionKey;
            var rowKeys = idx.Select(itm => itm.PrimaryRowKey).ToArray();
            return await tableStorage.GetDataAsync(partitionKey, rowKeys, pieces, filter);
        }

        public static async Task<T> FindByIndex<T>(this INoSQLTableStorage<AzureIndex> tableIndex,
            INoSQLTableStorage<T> tableStorage, string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            var indexEntity = await tableIndex.GetDataAsync(partitionKey, rowKey);
            if (indexEntity == null)
                return null;

            return await tableStorage.GetDataAsync(indexEntity);
        }


        public async static Task<T> GetDataAsync<T>(this INoSQLTableStorage<T> tableStorage, IAzureIndex index) where T : class, ITableEntity, new()
        {
            if (index == null)
                return null;

            return await tableStorage.GetDataAsync(index.PrimaryPartitionKey, index.PrimaryRowKey);
        }

        public async static Task<T> GetDataAsync<T>(this INoSQLTableStorage<T> tableStorage, INoSQLTableStorage<AzureIndex> indexTableStorage, 
            string indexPartitionKey, string indexRowKey) where T : class, ITableEntity, new()
        {
            var indexEntity = await indexTableStorage.GetDataAsync(indexPartitionKey, indexRowKey);
            return await tableStorage.GetDataAsync(indexEntity);
        }




        public async static Task<T> ReplaceAsync<T>(this INoSQLTableStorage<AzureIndex> indexTableStorage, string indexPartitionKey, string indexRowKey, INoSQLTableStorage<T> tableStorage, Func<T, T> action) where T : class, ITableEntity, new()
        {
            var indexEntity = await indexTableStorage.GetDataAsync(indexPartitionKey, indexRowKey);
            return await tableStorage.ReplaceAsync(indexEntity, action);
        }


        public async static Task<T> ReplaceAsync<T>(this INoSQLTableStorage<T> tableStorage, IAzureIndex index, Func<T, T> action) where T : class, ITableEntity, new()
        {
            if (index == null)
                return null;

            return await tableStorage.ReplaceAsync(index.PrimaryPartitionKey, index.PrimaryRowKey, action);
        }


        /// <summary>
        /// Delete index and entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="indexTableStorage">Table storage with indexes</param>
        /// <param name="indexPartitionKey">Index partition key</param>
        /// <param name="indexRowKey">Index row key</param>
        /// <param name="tableStorage">Table storage with entities</param>
        /// <returns>Deleted entity</returns>
        public static async Task<T> DeleteAsync<T>(this INoSQLTableStorage<AzureIndex> indexTableStorage,
            string indexPartitionKey, string indexRowKey, INoSQLTableStorage<T> tableStorage)
            where T : class, ITableEntity, new()
        {
            var indexEntity = await indexTableStorage.DeleteAsync(indexPartitionKey, indexRowKey);

            if (indexEntity == null)
                return null;

            return await tableStorage.DeleteAsync(indexEntity.PrimaryPartitionKey, indexEntity.PrimaryRowKey);
        }

    }
}

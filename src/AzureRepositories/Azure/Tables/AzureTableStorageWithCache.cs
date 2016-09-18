using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Log;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Azure.Tables
{
    /// <summary>
    ///     NoSql хранилище, которое хранит данные в кэше
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AzureTableStorageWithCache<T> : INoSQLTableStorage<T> where T : class, ITableEntity, new()
    {
        private readonly NoSqlTableInMemory<T> _cache;
        private readonly AzureTableStorage<T> _table;

        public AzureTableStorageWithCache(string connstionString, string tableName, ILog log)
        {
            _cache = new NoSqlTableInMemory<T>();
            _table = new AzureTableStorage<T>(connstionString, tableName, log);
            Init();
        }

        public async Task InsertAsync(T item, params int[] notLogCodes)
        {
            await _table.InsertAsync(item, notLogCodes);
            _cache.Insert(item, notLogCodes);
        }

        public async Task InsertAsync(IEnumerable<T> items)
        {
            await _table.InsertAsync(items);
            await _cache.InsertAsync(items);
        }

        public async Task InsertOrMergeAsync(T item)
        {
            await _table.InsertOrMergeAsync(item);
            _cache.InsertOrMerge(item);
        }

        public async Task<T> ReplaceAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            var result = await _table.ReplaceAsync(partitionKey, rowKey, item);
            _cache.Replace(partitionKey, rowKey, item);

            return result;
        }

        public async Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            var result = await _table.MergeAsync(partitionKey, rowKey, item);
            _cache.Merge(partitionKey, rowKey, item);
            return result;
        }

        public async Task InsertOrReplaceBatchAsync(IEnumerable<T> entites)
        {
            var myArray = entites as T[] ?? entites.ToArray();
            await _table.InsertOrReplaceBatchAsync(myArray);
            _cache.InsertOrReplaceBatch(myArray);
        }

        public async Task InsertOrReplaceAsync(T item)
        {
            await _table.InsertOrReplaceAsync(item);
            _cache.InsertOrReplace(item);
        }

        public async Task DeleteAsync(T item)
        {
            await _table.DeleteAsync(item);
            _cache.Delete(item);
        }

        public async Task<T> DeleteAsync(string partitionKey, string rowKey)
        {
            var result = await _table.DeleteAsync(partitionKey, rowKey);
            _cache.Delete(partitionKey, rowKey);
            return result;
        }

        public Task DeleteAsync(IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        public async Task CreateIfNotExistsAsync(T item)
        {
            await _table.CreateIfNotExistsAsync(item);
            await _cache.CreateIfNotExistsAsync(item);
        }

        public bool RecordExists(T item)
        {
            return _table.RecordExists(item);
        }

        public async Task DoBatchAsync(TableBatchOperation batch)
        {
            await _table.DoBatchAsync(batch);
            await _cache.DoBatchAsync(batch);
        }

        T INoSQLTableStorage<T>.this[string partition, string row] => _cache[partition, row];

        IEnumerable<T> INoSQLTableStorage<T>.this[string partition] => _cache[partition];

        public Task<T> GetDataAsync(string partition, string row) => _cache.GetDataAsync(partition, row);

        public Task<IList<T>> GetDataAsync(Func<T, bool> filter = null) => _cache.GetDataAsync(filter);

        public Task<IEnumerable<T>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys, int pieceSize = 100,
            Func<T, bool> filter = null)
            => _cache.GetDataAsync(partitionKey, rowKeys, pieceSize, filter);

        public Task<IEnumerable<T>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100,
            Func<T, bool> filter = null)
            => _cache.GetDataAsync(partitionKeys, pieceSize, filter);


        public Task<IEnumerable<T>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100,
            Func<T, bool> filter = null)
            => _cache.GetDataAsync(keys, pieceSize, filter);

        public async Task<T> GetTopRecordAsync(string partition) => await _cache.GetTopRecordAsync(partition);

        public async Task<IEnumerable<T>> GetTopRecordsAsync(string partition, int n)
            => await _cache.GetTopRecordsAsync(partition, n);

        public Task GetDataByChunksAsync(Func<IEnumerable<T>, Task> chunks)
            => _cache.GetDataByChunksAsync(chunks);

        public Task GetDataByChunksAsync(Action<IEnumerable<T>> chunks)
            => _cache.GetDataByChunksAsync(chunks);

        public Task GetDataByChunksAsync(string partitionKey, Action<IEnumerable<T>> chunks)
            => _cache.GetDataByChunksAsync(partitionKey, chunks);

        public Task ScanDataAsync(string partitionKey, Func<IEnumerable<T>, Task> chunk)
            => _cache.ScanDataAsync(partitionKey, chunk);

        public Task<T> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<T>, T> dataToSearch)
            => _cache.FirstOrNullViaScanAsync(partitionKey, dataToSearch);

        public Task<IEnumerable<T>> GetDataAsync(string partition, Func<T, bool> filter = null)
            => _cache.GetDataAsync(partition, filter);

        public Task<IEnumerable<T>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys)
            => _cache.GetDataRowKeysOnlyAsync(rowKeys);

        public Task<IEnumerable<T>> WhereAsyncc(TableQuery<T> rangeQuery, Func<T, Task<bool>> filter = null)
            => _table.WhereAsyncc(rangeQuery, filter);

        public Task<IEnumerable<T>> WhereAsync(TableQuery<T> rangeQuery, Func<T, bool> filter = null)
            => _table.WhereAsync(rangeQuery, filter);

        public Task ExecuteAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> yieldResult)
            => _table.ExecuteAsync(rangeQuery, yieldResult);


        public IEnumerator<T> GetEnumerator()
            => _cache.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _cache.GetEnumerator();

        private void Init()
        {
            // Вычитаем вообще все элементы в кэш
            foreach (var item in _table)
                _cache.Insert(item);
        }

        public IEnumerable<T> GetData(Func<T, bool> filter = null) => _cache.GetData(filter);

        public IEnumerable<T> GetData(string partitionKey, Func<T, bool> filter = null)
            => _cache.GetData(partitionKey, filter);
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace AzureRepositories.Azure.Tables
{
    public class AzureTableStorageLocal<T> : INoSQLTableStorage<T> where T : class, ITableEntity, new()
    {
        private readonly string _prefix;
        private readonly string _tableName;

        public AzureTableStorageLocal(string prefix, string tableName)
        {
            _prefix = prefix;
            _tableName = tableName;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetHttpReqest(null, null).Result.Cast<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetHttpReqest(null, null).Result.GetEnumerator();
        }

        public Task InsertAsync(T item, params int[] notLogCodes)
        {
            return PostHttpReqest(item.PartitionKey, item.RowKey, item);
        }

        public async Task InsertAsync(IEnumerable<T> items)
        {
            foreach (var entity in items)
                await InsertAsync(entity);
        }

        public Task InsertOrMergeAsync(T item)
        {
            return PostHttpReqest(item.PartitionKey, item.RowKey, item);
        }

        public async Task<T> ReplaceAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            var itm = await GetDataAsync(partitionKey, rowKey);

            itm = item(itm);

            await PostHttpReqest(partitionKey, rowKey, itm);
            return itm;
        }

        public Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            return ReplaceAsync(partitionKey, rowKey, item);
        }

        public async Task InsertOrReplaceBatchAsync(IEnumerable<T> entites)
        {
            foreach (var entity in entites)
                await InsertAsync(entity);
        }

        public Task InsertOrReplaceAsync(T item)
        {
            return PostHttpReqest(item.PartitionKey, item.RowKey, item);
        }

        public Task DeleteAsync(T item)
        {
            return DeleteHttpReqest(item.PartitionKey, item.RowKey);
        }

        public async Task<T> DeleteAsync(string partitionKey, string rowKey)
        {
            var item = await GetDataAsync(partitionKey, rowKey);
            if (item != null)
                await DeleteHttpReqest(partitionKey, rowKey);
            return item;
        }

        public async Task DeleteAsync(IEnumerable<T> items)
        {
            foreach (var entity in items)
                await DeleteAsync(entity);
        }

        public Task CreateIfNotExistsAsync(T item)
        {
            return InsertAsync(item);
        }

        public bool RecordExists(T item)
        {
            return GetDataAsync(item.PartitionKey, item.RowKey).Result != null;
        }

        public Task DoBatchAsync(TableBatchOperation batch)
        {
            throw new NotImplementedException();
        }

        T INoSQLTableStorage<T>.this[string partition, string row] => GetDataAsync(partition, row).Result;

        IEnumerable<T> INoSQLTableStorage<T>.this[string partition] => GetDataAsync(partition).Result;

        public async Task<T> GetDataAsync(string partition, string row)
        {
            var data = (await GetHttpReqest(partition, row)).FirstOrDefault();
            return data;
        }

        public async Task<IList<T>> GetDataAsync(Func<T, bool> filter = null)
        {
            var result = new List<T>();

            var data = await GetHttpReqest(null, null);

            foreach (var item in data)
            {
                if (filter == null)
                    result.Add(item);
                else if (filter(item))
                    result.Add(item);
            }

            return result;
        }

        public async Task<IEnumerable<T>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys,
            int pieceSize = 100, Func<T, bool> filter = null)
        {
            var result = new List<T>();
            var data = await GetHttpReqest(partitionKey, null);

            var rks = rowKeys.ToArray();

            foreach (var item in data)
            {
                if (rks.FirstOrDefault(rk => item.RowKey == rk) == null)
                    continue;

                if (filter == null)
                    result.Add(item);
                else if (filter(item))
                    result.Add(item);
            }


            return result;
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100,
            Func<T, bool> filter = null)
        {
            var result = new List<T>();
            var data = await GetHttpReqest(null, null);

            var pks = partitionKeys.ToArray();

            foreach (var item in data)
            {
                if (pks.FirstOrDefault(pk => item.PartitionKey == pk) == null)
                    continue;

                if (filter == null)
                    result.Add(item);
                else if (filter(item))
                    result.Add(item);
            }


            return result;
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100,
            Func<T, bool> filter = null)
        {
            var result = new List<T>();
            var data = await GetHttpReqest(null, null);

            var ks = keys.ToArray();

            foreach (var item in data)
            {
                if (ks.FirstOrDefault(k => item.PartitionKey == k.Item1 && item.RowKey == k.Item2) == null)
                    continue;

                if (filter == null)
                    result.Add(item);
                else if (filter(item))
                    result.Add(item);
            }


            return result;
        }

        public async Task<T> GetTopRecordAsync(string partition)
        {
            var data = await GetHttpReqest(partition, null);
            return data.FirstOrDefault();
        }

        public async Task<IEnumerable<T>> GetTopRecordsAsync(string partition, int n)
        {
            var data = await GetHttpReqest(partition, null);
            return data.Take(n);
        }

        public async Task GetDataByChunksAsync(Func<IEnumerable<T>, Task> chunks)
        {
            var data = await GetHttpReqest(null, null);
            await chunks(data);
        }

        public async Task GetDataByChunksAsync(Action<IEnumerable<T>> chunks)
        {
            var data = await GetHttpReqest(null, null);
            chunks(data);
        }

        public async Task GetDataByChunksAsync(string partitionKey, Action<IEnumerable<T>> chunks)
        {
            var data = await GetHttpReqest(partitionKey, null);
            chunks(data);
        }

        public async Task ScanDataAsync(string partitionKey, Func<IEnumerable<T>, Task> chunk)
        {
            var data = await GetHttpReqest(partitionKey, null);
            await chunk(data);
        }

        public async Task<T> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<T>, T> dataToSearch)
        {
            var data = await GetHttpReqest(partitionKey, null);
            return dataToSearch(data);
        }

        public async Task<IEnumerable<T>> GetDataAsync(string partition, Func<T, bool> filter = null)
        {
            var result = new List<T>();
            var data = await GetHttpReqest(partition, null);

            foreach (var item in data)
            {
                if (filter == null)
                    result.Add(item);
                else if (filter(item))
                    result.Add(item);
            }

            return result;
        }

        public async Task<IEnumerable<T>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys)
        {
            var data = await GetHttpReqest(null, null);

            var rks = rowKeys.ToArray();

            return data.Where(item => rks.FirstOrDefault(rk => item.RowKey == rk) != null).ToList();
        }

        public async Task<IEnumerable<T>> WhereAsyncc(TableQuery<T> rangeQuery, Func<T, Task<bool>> filter = null)
        {
            var data = Where(rangeQuery);
            var result = new List<T>();
            foreach (var itm in data)
            {
                if (filter == null || await filter(itm))
                    result.Add(itm);
            }

            return result;
        }

        public async Task<IEnumerable<T>> WhereAsync(TableQuery<T> rangeQuery, Func<T, bool> filter = null)
        {
            var whereInMemory = new WhereInMemory(rangeQuery.FilterString);

            var data = whereInMemory.PartitionKey == null
                ? await GetDataAsync()
                : await GetDataAsync(whereInMemory.PartitionKey);

            var result = data.Where(whereInMemory.PassRowKey);

            if (filter != null)
                result = result.Where(filter);

            return result.ToArray();
        }

        public Task ExecuteAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> yieldResult)
        {
            return Task.Run(() =>
            {
                var items = Where(rangeQuery);
                yieldResult(items);
            });
        }


        private string CompileRequestString(string partitionKey, string rowKey)
        {
            var requestString = _prefix + "/t/" + _tableName;

            if (partitionKey != null)
                requestString += "/" + partitionKey;

            if (rowKey != null)
                requestString += "/" + rowKey;

            return requestString;
        }


        private async Task<T[]> GetHttpReqest(string partitionKey, string rowKey)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));


                var oWebResponse = await client.GetAsync(CompileRequestString(partitionKey, rowKey));


                if ((int) oWebResponse.StatusCode == 201)
                    return null;

                var receiveStream = await oWebResponse.Content.ReadAsStreamAsync();

                if (receiveStream == null)
                    throw new Exception("ReceiveStream == null");

                var ms = new MemoryStream();
                receiveStream.CopyTo(ms);
                return Deserialize(ms.ToArray()).ToArray();
            }
        }

        private async Task<byte[]> PostHttpReqest(string partitionKey, string rowKey, T item)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

                var byteContent = new ByteArrayContent(Serialize(item));

                var oWebResponse = await client.PostAsync(CompileRequestString(partitionKey, rowKey), byteContent);
                var receiveStream = await oWebResponse.Content.ReadAsStreamAsync();

                if (receiveStream == null)
                    throw new Exception("ReceiveStream == null");

                var ms = new MemoryStream();
                receiveStream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private async Task<byte[]> DeleteHttpReqest(string partitionKey, string rowKey)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

                var oWebResponse = await client.DeleteAsync(CompileRequestString(partitionKey, rowKey));


                if ((int) oWebResponse.StatusCode == 201)
                    return null;

                var receiveStream = await oWebResponse.Content.ReadAsStreamAsync();

                if (receiveStream == null)
                    throw new Exception("ReceiveStream == null");

                var ms = new MemoryStream();
                receiveStream.CopyTo(ms);
                return ms.ToArray();
            }
        }


        private IEnumerable<T> Deserialize(byte[] array)
        {
            var result = new List<byte>(array.Length);

            foreach (var b in array)
            {
                if (b > 0)
                    result.Add(b);
                else if (result.Count > 0)
                {
                    var str = Encoding.UTF8.GetString(result.ToArray());
                    result.Clear();
                    yield return JsonConvert.DeserializeObject<T>(str);
                }
            }

            if (result.Count > 0)
            {
                var str = Encoding.UTF8.GetString(result.ToArray());
                yield return JsonConvert.DeserializeObject<T>(str);
            }
        }

        private byte[] Serialize(T item)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
        }

        public void Insert(T item, params int[] notLogCodes)
        {
            InsertAsync(item, notLogCodes).Wait();
        }

        public void InsertOrMerge(T item)
        {
            PostHttpReqest(item.PartitionKey, item.RowKey, item).Wait();
        }

        public T Replace(string partitionKey, string rowKey, Func<T, T> item)
        {
            return ReplaceAsync(partitionKey, rowKey, item).Result;
        }

        public void InsertOrReplaceBatch(IEnumerable<T> entites)
        {
            foreach (var entity in entites)
                Insert(entity);
        }

        public T Merge(string partitionKey, string rowKey, Func<T, T> item)
        {
            return Replace(partitionKey, rowKey, item);
        }

        public void InsertOrReplace(T item)
        {
            InsertOrReplaceAsync(item).Wait();
        }

        public void Delete(T item)
        {
            DeleteAsync(item).Wait();
        }

        public T Delete(string partitionKey, string rowKey)
        {
            return DeleteAsync(partitionKey, rowKey).Result;
        }

        public void DoBatch(TableBatchOperation batch)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> GetData(Func<T, bool> filter = null)
        {
            return GetDataAsync(filter).Result;
        }

        public IEnumerable<T> GetData(string partitionKey, Func<T, bool> filter = null)
        {
            return GetDataAsync(partitionKey, filter).Result;
        }

        public IEnumerable<T> Where(TableQuery<T> rangeQuery, Func<T, bool> filter = null)
        {
            var whereInMemory = new WhereInMemory(rangeQuery.FilterString);

            var data = whereInMemory.PartitionKey == null
                ? this.ToArray()
                : (this as INoSQLTableStorage<T>)[whereInMemory.PartitionKey];

            var result = data.Where(whereInMemory.PassRowKey);

            if (filter != null)
                result = result.Where(filter);

            return result.ToArray();
        }
    }
}
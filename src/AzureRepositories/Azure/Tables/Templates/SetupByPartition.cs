using System;
using System.Globalization;
using System.Threading.Tasks;
using Core.Log;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Azure.Tables.Templates
{
    public class SetupEntity : TableEntity
    {
        public string Value { get; set; }

        public static SetupEntity Create(string partitionKey, string rowKey, string value)
        {
            return new SetupEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                Value = value
            };
        }
    }


    public class SetupByPartitionEntity : TableEntity
    {
        public string Value { get; set; }
    }

    public abstract class NoSqlSetupByPartition
    {
        private readonly INoSQLTableStorage<SetupByPartitionEntity> _tableStorage;

        protected NoSqlSetupByPartition(INoSQLTableStorage<SetupByPartitionEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }


        public async Task<string> GetValueAsync(string partition, string field)
        {
            var entity = await _tableStorage.GetDataAsync(partition, field);
            return entity?.Value;
        }

        public Task SetValueAsync(string partition, string field, string value)
        {
            var entity = new SetupByPartitionEntity {PartitionKey = partition, RowKey = field, Value = value};

            return _tableStorage.InsertOrReplaceAsync(entity);
        }

        public Task SetValueAsync<T>(string partition, string field, T value)
        {
            return SetValueAsync(partition, field,
                (string) Convert.ChangeType(value, typeof(string), CultureInfo.InvariantCulture));
        }

        public async Task<T> GetValueAsync<T>(string partition, string field, T @default)
        {
            var resultStr = await GetValueAsync(partition, field);
            if (resultStr == null)
                return @default;

            try
            {
                return (T) Convert.ChangeType(resultStr, typeof(T), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return @default;
            }
        }
    }


    public class AzureSetupByPartition : NoSqlSetupByPartition
    {
        public AzureSetupByPartition(string connStr, string tableName, ILog log)
            : base(new AzureTableStorage<SetupByPartitionEntity>(connStr, tableName, log))
        {
        }
    }

    public class NoSqlSetupByPartitionInMemory : NoSqlSetupByPartition
    {
        public NoSqlSetupByPartitionInMemory()
            : base(new NoSqlTableInMemory<SetupByPartitionEntity>())
        {
        }
    }
}
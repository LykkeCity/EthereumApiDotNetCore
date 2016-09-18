using System.Threading.Tasks;
using Core.Log;
using Core.Utils;

namespace AzureRepositories.Azure.Tables.Templates
{

    public interface INoSqlTableForSetup
    {
        Task<T> GetValueAsync<T>(string field, T defaultValue);
        Task SetValueAsync<T>(string field, T value);

    }

    public abstract class NoSqlTableForSetupAbstract : INoSqlTableForSetup
    {
        private readonly NoSqlSetupByPartition _tableStorage;

        protected NoSqlTableForSetupAbstract(NoSqlSetupByPartition tableStorage)
        {
            _tableStorage = tableStorage;
            Partition = DefaultPartition;
        }

        public const string DefaultPartition = "Setup";
        public string Partition { get; set; }
        public string Field { get; set; }

        public string this[string field]
        {
            get
            {
                return _tableStorage.GetValueAsync(Partition, field).RunSync();

            }
            set
            {
                _tableStorage.SetValueAsync(Partition, field, value).RunSync();
            }
        }

        public Task<T> GetValueAsync<T>(string field, T defaultValue)
        {
            return _tableStorage.GetValueAsync(Partition, field, defaultValue);
        }

        public Task SetValueAsync<T>(string field, T value)
        {
            return _tableStorage.SetValueAsync(Partition, field, value);
        }
    }

    public class NoSqlTableForSetup : NoSqlTableForSetupAbstract
    {
        public NoSqlTableForSetup(string connStr, string tableName, ILog log) :
            base(new AzureSetupByPartition(connStr, tableName, log))
        {
        }

    }

    public class NoSqlTableForSetupInMemory : NoSqlTableForSetupAbstract
    {
        public NoSqlTableForSetupInMemory() :
            base(new NoSqlSetupByPartitionInMemory())
        {
        }

    }

}

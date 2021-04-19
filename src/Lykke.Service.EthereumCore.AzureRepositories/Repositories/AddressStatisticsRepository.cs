using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class AddressStatisticsEntity : TableEntity, IAddressStatistics
    {
        public string Address
        {
            get
            {
                return this.RowKey;
            }
            set
            {
                RowKey = value;
            }
        }

        public string TotalCashouts  { get; set; }
        public string FailedCashouts { get; set;}

        public static string GetPartitionKey()
        {
            return $"AddressStatistics";
        }

        public static string GetRowKey(string address)
        {
            return address.ToLower();
        }

        public static AddressStatisticsEntity Create(IAddressStatistics statistics)
        {
            return new AddressStatisticsEntity
            {
                PartitionKey = GetPartitionKey(),
                Address = GetRowKey(statistics.Address),
                FailedCashouts = statistics.FailedCashouts,
                TotalCashouts = statistics.TotalCashouts,
            };
        }
    }

    public class AddressStatisticsRepository : IAddressStatisticsRepository
    {
        private readonly INoSQLTableStorage<AddressStatisticsEntity> _table;

        public AddressStatisticsRepository(INoSQLTableStorage<AddressStatisticsEntity> table)
        {
            _table = table;
        }

        public async Task DeleteAsync(string address)
        {
            await _table.DeleteIfExistAsync(AddressStatisticsEntity.GetPartitionKey(), AddressStatisticsEntity.GetRowKey(address));
        }

        public async Task<IAddressStatistics> GetAsync(string address)
        {
            var entity = await _table.GetDataAsync(AddressStatisticsEntity.GetPartitionKey(), AddressStatisticsEntity.GetRowKey(address));

            return entity;
        }

        public async Task SaveAsync(IAddressStatistics address)
        {
            var entity = AddressStatisticsEntity.Create(address);

            await _table.InsertOrReplaceAsync(entity);
        }
    }
}

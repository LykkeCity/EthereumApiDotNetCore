using System.Numerics;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Repositories
{
    public class GasPriceRepository : IGasPriceRepository
    {
        private const string PartitionKey = "GasPrice";
        private const string RowKey = "GasPrice";

        private readonly INoSQLTableStorage<GasPriceEntity> _table;

        public GasPriceRepository(INoSQLTableStorage<GasPriceEntity> table)
        {
            _table = table;
        }


        public async Task<IGasPrice> GetAsync()
        {
            var entity = await _table.GetDataAsync(PartitionKey, RowKey);

            if (entity != null)
            {
                return new GasPrice
                {
                    Max = BigInteger.Parse(entity.Max),
                    Min = BigInteger.Parse(entity.Min)
                };
            }
            else
            {
                return new GasPrice
                {
                    Max = 120000000000,
                    Min = 100000000000
                };
            }
        }

        public async Task SetAsync(IGasPrice gasPrice)
        {
            var entity = new GasPriceEntity
            {
                Max = gasPrice.Max.ToString(),
                Min = gasPrice.Min.ToString(),
                PartitionKey = PartitionKey,
                RowKey = RowKey
            };

            await _table.InsertOrReplaceAsync(entity);
        }
    }

    public class GasPriceEntity : TableEntity
    {
        public string Max { get; set; }

        public string Min { get; set; }
    }
}
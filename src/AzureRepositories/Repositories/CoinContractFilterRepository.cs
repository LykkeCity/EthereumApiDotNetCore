using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;

namespace AzureRepositories.Repositories
{
    public class CoinContractFilterEntity : TableEntity, ICoinContractFilter
    {
        public static string GeneratePartitionKey()
        {
            return "CoinContractFilter";
        }

        public string EventName { get; set; }
        public string CoinName { get; set; }
        public string ContractAddress { get; set; }
        public string Filter => RowKey;

        public static CoinContractFilterEntity Create(ICoinContractFilter filter)
        {
            return new CoinContractFilterEntity
            {
                ContractAddress = filter.ContractAddress,
                EventName = filter.EventName,
                RowKey = filter.Filter,
                PartitionKey = GeneratePartitionKey(),
                CoinName = filter.CoinName
            };
        }

    }


    public class CoinContractFilterRepository : ICoinContractFilterRepository
    {
        private readonly INoSQLTableStorage<CoinContractFilterEntity> _table;

        public CoinContractFilterRepository(INoSQLTableStorage<CoinContractFilterEntity> table)
        {
            _table = table;
        }


        public async Task AddFilterAsync(ICoinContractFilter filter)
        {
            var entity = CoinContractFilterEntity.Create(filter);
            await _table.InsertAsync(entity);
        }

        public async Task<IEnumerable<ICoinContractFilter>> GetListAsync()
        {
            return await _table.GetDataAsync();
        }

        public async Task Clear()
        {
            await _table.DeleteAsync(await _table.GetDataAsync());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;
using System.Numerics;

namespace AzureRepositories.Repositories
{
    public class CashinEventEntity : TableEntity, ICashinEvent
    {
        public string CoinAdapterAddress { get; set; }
        public string TransactionHash { get { return this.RowKey; } set { RowKey = value; } }
        public string UserAddress { get; set; }
        public string Amount { get; set; }
        public string ContractAddress { get; set; }

        public static string GetPartitionKey()
        {
            return "CashinEvent";
        }

        public static CashinEventEntity CreateEntity(ICashinEvent cashinEvent)
        {
            return new CashinEventEntity
            {
                PartitionKey = GetPartitionKey(),
                TransactionHash = cashinEvent.TransactionHash,
                Amount = cashinEvent.Amount,
                CoinAdapterAddress = cashinEvent.CoinAdapterAddress,
                UserAddress = cashinEvent.UserAddress,
            };
        }
    }


    public class CashinEventRepository : ICashinEventRepository
    {
        private readonly INoSQLTableStorage<CashinEventEntity> _table;

        public CashinEventRepository(INoSQLTableStorage<CashinEventEntity> table)
        {
            _table = table;
        }

        public async Task<ICashinEvent> GetAsync(string transactionHash)
        {
            var entity = await _table.GetDataAsync(CashinEventEntity.GetPartitionKey(), transactionHash);

            return entity;
        }

        public async Task InsertAsync(ICashinEvent cashinEvent)
        {
            var @event = CashinEventEntity.CreateEntity(cashinEvent);

            await _table.InsertOrReplaceAsync(@event);
        }

        public Task SyncedToAsync(BigInteger to)
        {
            throw new NotImplementedException();
        }
    }
}

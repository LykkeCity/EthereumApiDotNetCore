using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;

namespace AzureRepositories.Repositories
{
    public class OwnerEntity : TableEntity, IOwner
    {
        public const string Key = "Owner";

        public string Address
        {
            get
            {
                return this.RowKey;
            }
            set
            {
                this.RowKey = value;
            }
        }

        public static OwnerEntity CreateEntity(IOwner owner)
        {
            return new OwnerEntity()
            {
                PartitionKey = Key,
                Address = owner.Address?.ToLower()
            };
        }
    }


    public class OwnerRepository : IOwnerRepository
    {
        private readonly INoSQLTableStorage<OwnerEntity> _table;

        public OwnerRepository(INoSQLTableStorage<OwnerEntity> table)
        {
            _table = table;
        }

        public async Task<IEnumerable<IOwner>> GetAllAsync()
        {
            var all = await _table.GetDataAsync(OwnerEntity.Key);

            return all;
        }

        public async Task RemoveAsync(string address)
        {
            await _table.DeleteIfExistAsync(OwnerEntity.Key, address?.ToLower());
        }

        public async Task SaveAsync(IOwner owner)
        {
            var entity = OwnerEntity.CreateEntity(owner);
            await _table.InsertOrReplaceAsync(entity);
        }
    }
}

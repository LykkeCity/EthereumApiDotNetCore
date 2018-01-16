using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using System.Numerics;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class BlackListAddressEntity : TableEntity, IBlackListAddress
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

        public static string GetPartitionKey()
        {
            return $"BlackListAddress";
        }

        public static string GetRowKey(string address)
        {
            return address.ToLower();
        }

        public static BlackListAddressEntity Create(IBlackListAddress blackListAddress)
        {
            return new BlackListAddressEntity
            {
                PartitionKey = GetPartitionKey(),
                Address = GetRowKey(blackListAddress.Address),
            };
        }
    }

    public class BlackListAddressesRepository : IBlackListAddressesRepository
    {
        private readonly INoSQLTableStorage<BlackListAddressEntity> _table;

        public BlackListAddressesRepository(INoSQLTableStorage<BlackListAddressEntity> table)
        {
            _table = table;
        }

        public async Task DeleteAsync(string address)
        {
            await _table.DeleteIfExistAsync(BlackListAddressEntity.GetPartitionKey(), BlackListAddressEntity.GetRowKey(address));
        }

        public async Task<IBlackListAddress> GetAsync(string address)
        {
            var entity = await _table.GetDataAsync(BlackListAddressEntity.GetPartitionKey(), BlackListAddressEntity.GetRowKey(address));

            return entity;
        }

        public async Task SaveAsync(IBlackListAddress address)
        {
            var entity = BlackListAddressEntity.Create(address);

            await _table.InsertOrReplaceAsync(entity);
        }
    }
}

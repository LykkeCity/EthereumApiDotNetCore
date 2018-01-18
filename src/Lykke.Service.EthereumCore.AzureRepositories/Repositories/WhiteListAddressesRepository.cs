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
    public class WhiteListAddressesEntity : TableEntity, IWhiteListAddress
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
            return $"WhiteListAddress";
        }

        public static string GetRowKey(string address)
        {
            return address.ToLower();
        }

        public static WhiteListAddressesEntity Create(IWhiteListAddress whiteListAddress)
        {
            return new WhiteListAddressesEntity
            {
                PartitionKey = GetPartitionKey(),
                Address = GetRowKey(whiteListAddress.Address),
            };
        }
    }

    public class WhiteListAddressesRepository : IWhiteListAddressesRepository
    {
        private readonly INoSQLTableStorage<WhiteListAddressesEntity> _table;

        public WhiteListAddressesRepository(INoSQLTableStorage<WhiteListAddressesEntity> table)
        {
            _table = table;
        }

        public async Task DeleteAsync(string address)
        {
            await _table.DeleteIfExistAsync(WhiteListAddressesEntity.GetPartitionKey(), WhiteListAddressesEntity.GetRowKey(address));
        }

        public async Task<IWhiteListAddress> GetAsync(string address)
        {
            var entity = await _table.GetDataAsync(WhiteListAddressesEntity.GetPartitionKey(), WhiteListAddressesEntity.GetRowKey(address));

            return entity;
        }

        public async Task SaveAsync(IWhiteListAddress address)
        {
            var entity = WhiteListAddressesEntity.Create(address);

            await _table.InsertOrReplaceAsync(entity);
        }
    }
}

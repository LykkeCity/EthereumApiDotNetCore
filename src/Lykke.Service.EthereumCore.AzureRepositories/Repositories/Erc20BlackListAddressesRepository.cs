﻿using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using Common;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class Erc20BlackListAddressesEntity : TableEntity, IErc20BlackListAddress
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

        public static string GetPartitionKey(string address)
        {
            return address.CalculateHexHash32(3);
        }

        public static string GetRowKey(string address)
        {
            return address.ToLower();
        }

        public static Erc20BlackListAddressesEntity Create(IErc20BlackListAddress whiteListAddress)
        {
            return new Erc20BlackListAddressesEntity
            {
                PartitionKey = GetPartitionKey(whiteListAddress.Address),
                Address = GetRowKey(whiteListAddress.Address),
            };
        }
    }

    public class Erc20BlackListAddressesRepository : IErc20BlackListAddressesRepository
    {
        private readonly INoSQLTableStorage<Erc20BlackListAddressesEntity> _table;

        public Erc20BlackListAddressesRepository(INoSQLTableStorage<Erc20BlackListAddressesEntity> table)
        {
            _table = table;
        }

        public async Task DeleteAsync(string address)
        {
            await _table.DeleteIfExistAsync(Erc20BlackListAddressesEntity.GetPartitionKey(address), Erc20BlackListAddressesEntity.GetRowKey(address));
        }

        public async Task<IErc20BlackListAddress> GetAsync(string address)
        {
            var entity = await _table.GetDataAsync(Erc20BlackListAddressesEntity.GetPartitionKey(address), Erc20BlackListAddressesEntity.GetRowKey(address));

            return entity;
        }

        public async Task SaveAsync(IErc20BlackListAddress address)
        {
            var entity = Erc20BlackListAddressesEntity.Create(address);

            await _table.InsertOrReplaceAsync(entity);
        }
    }
}

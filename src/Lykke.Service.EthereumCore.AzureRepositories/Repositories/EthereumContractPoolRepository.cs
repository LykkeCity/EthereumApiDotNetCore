using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class EthereumContractPoolEntity : TableEntity, IEthereumContractPool
    {
        public const string Key = "EthereumPool";

        public string TxHashes { get; set; }

        public static EthereumContractPoolEntity CreateCoinEntity(IEthereumContractPool pool)
        {
            return new EthereumContractPoolEntity
            {
                RowKey = Key,
                PartitionKey = Key,
                TxHashes = pool.TxHashes
            };
        }
    }

    public class EthereumCreatedContractEntity : TableEntity
    {
        public const string Key = "CreatedAddress";


        public static EthereumCreatedContractEntity CreateEntity(string contractAddress)
        {
            return new EthereumCreatedContractEntity
            {
                RowKey = contractAddress,
                PartitionKey = Key,
            };
        }
    }


    public class EthereumContractPoolRepository : IEthereumContractPoolRepository
    {
        private readonly INoSQLTableStorage<EthereumContractPoolEntity> _table;
        private readonly INoSQLTableStorage<EthereumCreatedContractEntity> _createdContracts;

        public EthereumContractPoolRepository(
            INoSQLTableStorage<EthereumContractPoolEntity> table,
            INoSQLTableStorage<EthereumCreatedContractEntity> createdContracts)
        {
            _table = table;
            _createdContracts = createdContracts;
        }

        public async Task SaveAsync(IEthereumContractPool pool)
        {
            var entity = EthereumContractPoolEntity.CreateCoinEntity(pool);

            await _table.InsertOrReplaceAsync(entity);
        }

        public async Task ClearAsync()
        {
            await _table.DeleteAsync(EthereumContractPoolEntity.Key, EthereumContractPoolEntity.Key);
        }

        public async Task<IEthereumContractPool> GetAsync()
        {
            var pool = await _table.GetDataAsync(EthereumContractPoolEntity.Key, EthereumContractPoolEntity.Key);

            return pool;
        }

        public async Task<bool> GetOrDefaultAsync(string contractAddress)
        {
            var val = await _createdContracts.GetDataAsync(EthereumCreatedContractEntity.Key, contractAddress.ToLower());

            return val != null;
        }

        public async Task InsertOrReplaceAsync(string contractAddress)
        {
            await _createdContracts.InsertOrReplaceAsync(EthereumCreatedContractEntity.CreateEntity(contractAddress.ToLower()));
        }
    }
}

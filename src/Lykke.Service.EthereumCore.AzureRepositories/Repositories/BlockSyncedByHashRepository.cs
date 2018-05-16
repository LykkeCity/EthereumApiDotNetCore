using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;
using System.Numerics;
using Org.BouncyCastle.Math;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class BlockSyncedByHashEntity : TableEntity, IBlockSyncedByHash
    {
        public string BlockNumber { get; set; }
        public string Partition { get { return this.PartitionKey; } set { this.PartitionKey = value; } }
        public string BlockHash { get { return this.RowKey; } set { this.RowKey = value; } }

        public static BlockSyncedByHashEntity CreateEntity(IBlockSyncedByHash blockSynced)
        {
            return new BlockSyncedByHashEntity
            {
                PartitionKey = blockSynced.Partition,
                BlockNumber = blockSynced.BlockNumber,
                Partition = blockSynced.Partition,
                BlockHash = blockSynced.BlockHash?.ToLower() 
            };
        }
    }

    public class BlockSyncedByHashRepository : IBlockSyncedByHashRepository
    {
        private readonly INoSQLTableStorage<BlockSyncedByHashEntity> _table;
        private readonly INoSQLTableStorage<AzureIndex> _index;
        private readonly string _lastSyncedPartition = "LastSynced";

        public BlockSyncedByHashRepository(INoSQLTableStorage<BlockSyncedByHashEntity> table, INoSQLTableStorage<AzureIndex> index)
        {
            _table = table;
            _index = index;
        }

        public async Task InsertAsync(IBlockSyncedByHash block)
        {
            var entity = BlockSyncedByHashEntity.CreateEntity(block);
            var index = new AzureIndex(_lastSyncedPartition, block.Partition, entity);
            await _table.InsertOrReplaceAsync(entity);
            await _index.InsertOrReplaceAsync(index);
        }

        public async Task<IBlockSyncedByHash> GetByPartitionAndHashAsync(string partition, string hash)
        {
            var entity = await _table.GetDataAsync(partition, hash?.ToLower());

            return entity;
        }

        public async Task DeleteByPartitionAndHashAsync(string partition, string hash)
        {
            await _table.DeleteIfExistAsync(partition, hash?.ToLower());
        }

        public async Task<IBlockSyncedByHash> GetLastSyncedAsync(string partition)
        {
            var index = await _index.GetDataAsync(_lastSyncedPartition, partition);

            if (index == null)
                return null;

            var entity = await _table.GetDataAsync(index);

            return entity;
        }
    }
}

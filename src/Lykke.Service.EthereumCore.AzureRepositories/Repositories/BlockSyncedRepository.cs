using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class BlockSyncedEntity : TableEntity, IBlockSynced
    {
        public string CoinAdapterAddress { get; set; }
        public string BlockNumber { get { return this.RowKey; } set { this.RowKey = value; } }

        public static string GetPartitionKey(string adapterAddress)
        {
            return $"CashinEvent_{adapterAddress}";
        }

        public static BlockSyncedEntity CreateEntity(IBlockSynced blockSynced)
        {
            return new BlockSyncedEntity
            {
                PartitionKey = GetPartitionKey(blockSynced.CoinAdapterAddress),
                BlockNumber = blockSynced.BlockNumber,
                CoinAdapterAddress = blockSynced.CoinAdapterAddress
            };
        }
    }


    public class BlockSyncedRepository : IBlockSyncedRepository
    {
        private readonly INoSQLTableStorage<BlockSyncedEntity> _table;
        private readonly INoSQLTableStorage<AzureIndex> _index;
        private readonly string _lastSyncedPartition = "LastSynced";

        public BlockSyncedRepository(INoSQLTableStorage<BlockSyncedEntity> table, INoSQLTableStorage<AzureIndex> index)
        {
            _table = table;
            _index = index;
        }

        public async Task<IBlockSynced> GetLastSyncedAsync(string coinAdapterAddress)
        {
            var index = await _index.GetDataAsync(_lastSyncedPartition, coinAdapterAddress);
            var entity = await _table.GetDataAsync(index);

            return entity;
        }

        public async Task InsertAsync(IBlockSynced block)
        {
            var entity = BlockSyncedEntity.CreateEntity(block);
            var index = new AzureIndex(_lastSyncedPartition, block.CoinAdapterAddress, entity);
            await _table.InsertOrReplaceAsync(entity);
            await _index.InsertOrReplaceAsync(index);
        }

        public async Task ClearAllIndexes()
        {
            IEnumerable<AzureIndex> indexes = await _index.GetDataAsync();

            foreach (var item in indexes)
            {
                await _index.DeleteIfExistAsync(item.PartitionKey, item.RowKey);
            }
        }

        public async Task ClearForAdapter(string coinAdapterAddress)
        {
            AzureIndex index = await _index.GetDataAsync(_lastSyncedPartition, coinAdapterAddress);

            await _index.DeleteIfExistAsync(index.PartitionKey, index.RowKey);
        }
    }
}

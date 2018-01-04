using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class CoinEntity : TableEntity, ICoin
    {
        public const string Key = "Asset";

        public string Id => RowKey;
        public string Blockchain { get; set; }
        public string AdapterAddress { get; set; }
        public int Multiplier { get; set; }
        public bool BlockchainDepositEnabled { get; set; }

        public string Name { get; set; }

        public bool ContainsEth { get; set; }

        public string ExternalTokenAddress { get; set; }
        public string DeployedTransactionHash { get; set; }

        public static CoinEntity CreateCoinEntity(ICoin coin)
        {
            return new CoinEntity
            {
                Name = coin.Name,
                AdapterAddress = coin.AdapterAddress,
                RowKey = coin.Id,
                Multiplier = coin.Multiplier,
                Blockchain = coin.Blockchain,
                PartitionKey = Key,
                BlockchainDepositEnabled = coin.BlockchainDepositEnabled,
                ContainsEth = coin.ContainsEth,
                ExternalTokenAddress = coin.ExternalTokenAddress,
                DeployedTransactionHash = coin.DeployedTransactionHash
            };
        }
    }


    public class CoinRepository : ICoinRepository
    {
        private readonly INoSQLTableStorage<CoinEntity> _table;
        private readonly INoSQLTableStorage<AzureIndex> _addressIndex;
        private const string _addressIndexName = "AddressIndex";

        public CoinRepository(INoSQLTableStorage<CoinEntity> table, INoSQLTableStorage<AzureIndex> addressIndex)
        {
            _addressIndex = addressIndex;
            _table = table;
        }

        public async Task<ICoin> GetCoin(string coinId)
        {
            var coin = await _table.GetDataAsync(CoinEntity.Key, coinId);
            if (coin == null)
                throw new Exception("Unknown coin name - " + coinId);

            return coin;
        }

        public async Task InsertOrReplace(ICoin coin)
        {
            var entity = CoinEntity.CreateCoinEntity(coin);
            var index = AzureIndex.Create(_addressIndexName, coin.AdapterAddress, entity);

            await _table.InsertOrReplaceAsync(entity);
            await _addressIndex.InsertAsync(index);
        }

        public async Task<ICoin> GetCoinByAddress(string coinAddress)
        {
            AzureIndex index = await _addressIndex.GetDataAsync(_addressIndexName, coinAddress);
            if (index == null)
                return null;
            var coin = await _table.GetDataAsync(index);

            return coin;
        }

        public async Task ProcessAllAsync(Func<IEnumerable<ICoin>, Task> processAction)
        {
            Func<IEnumerable<CoinEntity>, Task> function = async (items) =>
            {
                await processAction(items);
            };

            await _table.GetDataByChunksAsync(function);
        }

        public async Task<IEnumerable<ICoin>> GetAll()
        {
            var all = await _table.GetDataAsync(CoinEntity.Key);

            return all;
        }
    }
}

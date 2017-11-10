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
    public class HotWalletCashoutEntity : TableEntity, IHotWalletCashout
    {
        public const string Key = "Asset";

        public string OperationId
        {
            get
            {
                return RowKey;
            }
            set
            {
                RowKey = value;
            }
        }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public BigInteger Amount
        {
            get
            {
                return BigInteger.Parse(AmountStr);
            }
            set
            {
                AmountStr = value.ToString();
            }
        }
        public string AmountStr { get; set; }
        public string TokenAddress { get; set; }

        public static HotWalletCashoutEntity CreateCoinEntity(IHotWalletCashout cashout)
        {
            return new HotWalletCashoutEntity()
            {

            };
        }
    }


    public class HotWalletCashoutRepository : IHotWalletCashoutRepository
    {
        private readonly INoSQLTableStorage<CoinEntity> _table;

        public HotWalletCashoutRepository(INoSQLTableStorage<CoinEntity> table)
        {
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

        public async Task<IEnumerable<IHotWalletCashout>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public async Task SaveAsync(IHotWalletCashout owner)
        {
            throw new NotImplementedException();
        }

        public async Task<IHotWalletCashout> GetAsync(string operationId)
        {
            throw new NotImplementedException();
        }
    }
}

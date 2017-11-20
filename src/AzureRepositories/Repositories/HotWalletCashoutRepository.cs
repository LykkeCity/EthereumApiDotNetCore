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
        public const string Key = "HotWalletCashout";

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

        public static HotWalletCashoutEntity CreateEntity(IHotWalletCashout cashout)
        {
            return new HotWalletCashoutEntity()
            {
                Amount = cashout.Amount,
                FromAddress = cashout.FromAddress,
                OperationId = cashout.OperationId,
                PartitionKey = Key,
                ToAddress = cashout.ToAddress,
                TokenAddress = cashout.TokenAddress,
            };
        }
    }


    public class HotWalletCashoutRepository : IHotWalletCashoutRepository
    {
        private readonly INoSQLTableStorage<HotWalletCashoutEntity> _table;

        public HotWalletCashoutRepository(INoSQLTableStorage<HotWalletCashoutEntity> table)
        {
            _table = table;
        }


        public async Task<IEnumerable<IHotWalletCashout>> GetAllAsync()
        {
            var all = await _table.GetDataAsync(HotWalletCashoutEntity.Key);

            return all;
        }

        public async Task SaveAsync(IHotWalletCashout cashout)
        {
            HotWalletCashoutEntity entity = HotWalletCashoutEntity.CreateEntity(cashout);

            await _table.InsertOrReplaceAsync(entity);
        }

        public async Task<IHotWalletCashout> GetAsync(string operationId)
        {
            var entity = await _table.GetDataAsync(HotWalletCashoutEntity.Key, operationId);

            return entity;
        }
    }
}

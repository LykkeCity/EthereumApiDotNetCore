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
    public class HotWalletCashoutEntity : TableEntity, IHotWalletOperation
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
        public HotWalletOperationType OperationType
        {
            get
            {
                return (HotWalletOperationType)Enum.Parse(typeof(HotWalletOperationType), OperationTypeStr);
            }

            set
            {
                OperationTypeStr = value.ToString();
            }
        }

        public string OperationTypeStr { get; set; }

        public static HotWalletCashoutEntity CreateEntity(IHotWalletOperation cashout)
        {
            return new HotWalletCashoutEntity()
            {
                Amount = cashout.Amount,
                FromAddress = cashout.FromAddress,
                OperationId = cashout.OperationId,
                PartitionKey = Key,
                ToAddress = cashout.ToAddress,
                TokenAddress = cashout.TokenAddress,
                OperationType = cashout.OperationType
            };
        }
    }


    public class HotWalletOperationRepository : IHotWalletOperationRepository
    {
        private readonly INoSQLTableStorage<HotWalletCashoutEntity> _table;

        public HotWalletOperationRepository(INoSQLTableStorage<HotWalletCashoutEntity> table)
        {
            _table = table;
        }


        public async Task<IEnumerable<IHotWalletOperation>> GetAllAsync()
        {
            var all = await _table.GetDataAsync(HotWalletCashoutEntity.Key);

            return all;
        }

        public async Task SaveAsync(IHotWalletOperation cashout)
        {
            HotWalletCashoutEntity entity = HotWalletCashoutEntity.CreateEntity(cashout);

            await _table.InsertOrReplaceAsync(entity);
        }

        public async Task<IHotWalletOperation> GetAsync(string operationId)
        {
            var entity = await _table.GetDataAsync(HotWalletCashoutEntity.Key, operationId);

            return entity;
        }
    }
}

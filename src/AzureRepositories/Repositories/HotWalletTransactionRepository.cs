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
    #region Entities

    public class HotWalletCashoutTransactionOpIdPartitionEntity : TableEntity, IHotWalletTransaction
    {
        public const string Key = "HotWalletCashoutTransactionOpId";

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

        public string TransactionHash { get; set; }

        public static HotWalletCashoutTransactionOpIdPartitionEntity CreateEntity(IHotWalletTransaction transaction)
        {
            return new HotWalletCashoutTransactionOpIdPartitionEntity()
            {
                PartitionKey = Key,
                OperationId = transaction.OperationId,
                TransactionHash = transaction.TransactionHash,
            };
        }
    }

    public class HotWalletCashoutTransactionHashPartitionEntity : TableEntity, IHotWalletTransaction
    {
        public const string Key = "HotWalletCashoutTransactionHash";

        public string OperationId { get; set; }

        public string TransactionHash
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

        public static HotWalletCashoutTransactionHashPartitionEntity CreateEntity(IHotWalletTransaction transaction)
        {
            return new HotWalletCashoutTransactionHashPartitionEntity()
            {
                PartitionKey = Key,
                OperationId = transaction.OperationId,
                TransactionHash = transaction.TransactionHash,
            };
        }
    }


    #endregion

    public class HotWalletTransactionRepository : IHotWalletTransactionRepository
    {
        private readonly INoSQLTableStorage<HotWalletCashoutTransactionOpIdPartitionEntity> _tableOpId;
        private readonly INoSQLTableStorage<HotWalletCashoutTransactionHashPartitionEntity> _tableTrHash;

        public HotWalletTransactionRepository(INoSQLTableStorage<HotWalletCashoutTransactionOpIdPartitionEntity> tableOpId,
            INoSQLTableStorage<HotWalletCashoutTransactionHashPartitionEntity> tableTrHash)
        {
            _tableOpId = tableOpId;
            _tableTrHash = tableTrHash;
        }


        public async Task SaveAsync(IHotWalletTransaction cashout)
        {
            HotWalletCashoutTransactionHashPartitionEntity entityHash = HotWalletCashoutTransactionHashPartitionEntity.CreateEntity(cashout);
            HotWalletCashoutTransactionOpIdPartitionEntity entityOpId = HotWalletCashoutTransactionOpIdPartitionEntity.CreateEntity(cashout);

            await _tableOpId.InsertOrReplaceAsync(entityOpId);
            await _tableTrHash.InsertOrReplaceAsync(entityHash);
        }

        public async Task<IHotWalletTransaction> GetByOperationIdAsync(string operationId)
        {
            var entity = await _tableOpId.GetDataAsync(HotWalletCashoutTransactionOpIdPartitionEntity.Key, operationId);

            return entity;
        }

        public async Task<IHotWalletTransaction> GetByTransactionHashAsync(string transactionHash)
        {
            var entity = await _tableOpId.GetDataAsync(HotWalletCashoutTransactionHashPartitionEntity.Key, transactionHash);

            return entity;
        }
    }
}

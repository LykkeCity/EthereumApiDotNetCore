using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using System.Numerics;

namespace AzureRepositories.Repositories
{
    public class PendingOperationEntity : TableEntity, IPendingOperation
    {
        public string OperationId
        {
            get
            {
                return this.RowKey;
            }
            set
            {
                this.RowKey = value;
            }
        }
        public string SignFrom { get; set; }
        public string SignTo { get; set; }
        public string CoinAdapterAddress { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public string Amount { get; set; }
        public string OperationType { get; set; }
        public string Change { get; set; }

        public static string GetPartitionKey()
        {
            return $"PendingOperation";
        }

        public static PendingOperationEntity Create(IPendingOperation match)
        {
            return new PendingOperationEntity
            {
                PartitionKey = GetPartitionKey(),
                OperationId = match.OperationId,
                Amount = match.Amount,
                CoinAdapterAddress = match.CoinAdapterAddress,
                SignFrom = match.SignFrom,
                SignTo = match.SignTo,
                ToAddress = match.ToAddress,
                FromAddress = match.FromAddress,
                OperationType = match.OperationType,
                Change = match.Change
            };
        }
    }

    public class PendingOperationRepository : IPendingOperationRepository
    {
        private readonly INoSQLTableStorage<PendingOperationEntity> _table;

        public PendingOperationRepository(INoSQLTableStorage<PendingOperationEntity> table)
        {
            _table = table;
        }

        public async Task<IPendingOperation> GetOperation(string operationId)
        {
            var match = await _table.GetDataAsync(PendingOperationEntity.GetPartitionKey(), operationId);

            return match;
        }

        public async Task InsertOrReplace(IPendingOperation pendingOp)
        {
            var entity = PendingOperationEntity.Create(pendingOp);

            await _table.InsertOrReplaceAsync(entity);
        }

        public Task ProcessAllAsync(Func<IEnumerable<IPendingOperation>, Task> processAction)
        {
            throw new NotImplementedException();
        }
    }
}

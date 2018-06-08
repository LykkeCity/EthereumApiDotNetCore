using AzureStorage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CashinReportGenerator
{
    public class EthererumPendingActionEntity : TableEntity
    {
        public static EthererumPendingActionEntity CreatePending(string clientId,
            string operationId)
        {
            return new EthererumPendingActionEntity
            {
                PartitionKey = clientId,
                RowKey = operationId,
                Timestamp = DateTimeOffset.UtcNow,
            };
        }

        public static EthererumPendingActionEntity CreateCompleted(string clientId,
            string operationId)
        {
            return new EthererumPendingActionEntity
            {
                PartitionKey = $"Completed_{clientId}",
                RowKey = operationId,
                Timestamp = DateTimeOffset.UtcNow,
            };
        }

        public static EthererumPendingActionEntity CreateUserAgreement(string clientId)
        {
            return new EthererumPendingActionEntity
            {
                PartitionKey = UserAgreementKey(clientId),
                RowKey = UserAgreementKey(clientId),
                Timestamp = DateTimeOffset.UtcNow,
            };
        }

        public static string UserAgreementKey(string clientId)
        {
            return $"AgreedToTrust_{clientId}";
        }

        public string ClientId => PartitionKey;
        public string OperationId => RowKey;
    }

    public class EthererumPendingActionsRepository
    {
        private readonly INoSQLTableStorage<EthererumPendingActionEntity> _tableStorage;

        public EthererumPendingActionsRepository(INoSQLTableStorage<EthererumPendingActionEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IEnumerable<string>> GetPendingAsync(string clientId)
        {
            var entities = await _tableStorage.GetDataAsync(clientId);

            return entities?.Select(x => x.OperationId);
        }

        public async Task<bool> GetUserAgreementAsync(string clientId)
        {
            var key = EthererumPendingActionEntity.UserAgreementKey(clientId);
            var agreement = await _tableStorage.GetDataAsync(key, key);

            return agreement != null;
        }

        public async Task SetUserAgreementAsync(string clientId)
        {
            var entity = EthererumPendingActionEntity.CreateUserAgreement(clientId);

            await _tableStorage.InsertAsync(entity);
        }

        public async Task CreateAsync(string clientId, string operationId)
        {
            var entity = EthererumPendingActionEntity.CreatePending(clientId, operationId);

            await _tableStorage.InsertAsync(entity);
        }

        public async Task CompleteAsync(string clientId, string operationId)
        {
            var entity = EthererumPendingActionEntity.CreateCompleted(clientId, operationId);

            await _tableStorage.InsertAsync(entity);
            await _tableStorage.DeleteIfExistAsync(clientId, operationId);

        }
    }
}

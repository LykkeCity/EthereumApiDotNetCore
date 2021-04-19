using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class OperationResubmittEntity : TableEntity, IOperationResubmitt
    {
        public static string GetPartitionKey()
        {
            return "OperationResubmitt";
        }

        public string OperationId
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        public int ResubmittCount
        {
            get; set;
        }

        public static OperationResubmittEntity Create(IOperationResubmitt match)
        {
            return new OperationResubmittEntity
            {
                PartitionKey = GetPartitionKey(),
                OperationId = match.OperationId,
                ResubmittCount = match.ResubmittCount
            };
        }
    }

    public class OperationResubmittRepository : IOperationResubmittRepository
    {
        private readonly INoSQLTableStorage<OperationResubmittEntity> _table;

        public OperationResubmittRepository(INoSQLTableStorage<OperationResubmittEntity> table)
        {
            _table = table;
        }

        public async Task<IOperationResubmitt> GetAsync(string operationId)
        {
            var entity = await _table.GetDataAsync(OperationResubmittEntity.GetPartitionKey(), operationId);

            return entity;
        }

        public async Task InsertOrReplaceAsync(IOperationResubmitt operation)
        {
            var entity = OperationResubmittEntity.Create(operation);

            await _table.InsertOrReplaceAsync(entity);
        }
    }
}

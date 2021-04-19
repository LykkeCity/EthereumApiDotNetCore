using System;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using System.Globalization;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class EventTraceEntity : TableEntity, IEventTrace
    {
        public static string GetPartitionKey(string operationId)
        {
            return $"EventTrace_{operationId}";
        }

        public string TimeKey => this.RowKey;
        public string OperationId { get; set; }
        public DateTime TraceDate => DateTime.Parse(RowKey, CultureInfo.InvariantCulture);
        public string Note { get; set; }

        public static EventTraceEntity CreateCoinEntity(IEventTrace trace)
        {
            return new EventTraceEntity
            {
                RowKey = trace.TraceDate.ToString("o", CultureInfo.InvariantCulture),
                PartitionKey = GetPartitionKey(trace.OperationId),
                OperationId = trace.OperationId,
                Note = trace.Note
            };
        }
    }

    public class EventTraceRepository : IEventTraceRepository
    {
        private readonly INoSQLTableStorage<EventTraceEntity> _table;

        public EventTraceRepository(INoSQLTableStorage<EventTraceEntity> table)
        {
            _table = table;
        }

        public async Task InsertAsync(IEventTrace trace)
        {
            var entity = EventTraceEntity.CreateCoinEntity(trace);

            await _table.InsertOrReplaceAsync(entity);
        }
    }
}

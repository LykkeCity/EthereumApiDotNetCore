using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class CoinEventEntity : TableEntity, ICoinEvent
    {
        public string CoinEventTypeStr { get; set; }

        public CoinEventType CoinEventType
        {
            get
            {
                return (CoinEventType)Enum.Parse(typeof(CoinEventType), CoinEventTypeStr);
            }

            set
            {
                CoinEventTypeStr = value.ToString();
            }
        }

        public string TransactionHash { get { return this.RowKey; } set { this.RowKey = value; } }

        public string ContractAddress { get; set; }

        public string FromAddress { get; set; }

        public string ToAddress { get; set; }

        public string Amount { get; set; }

        public string Additional { get; set; }

        public DateTime EventTime { get; set; }

        public bool Success { get; set; }

        public string OperationId { get; set; }

        public static string GetPartitionKey()
        {
            return "CoinEvent";
        }

        public static CoinEventEntity CreateEntity(ICoinEvent coinEvent)
        {
            return new CoinEventEntity
            {
                PartitionKey = GetPartitionKey(),
                TransactionHash = coinEvent.TransactionHash,
                Additional = coinEvent.Additional,
                Amount = coinEvent.Amount,
                CoinEventType = coinEvent.CoinEventType,
                ContractAddress = coinEvent.ContractAddress,
                Success = coinEvent.Success,
                ToAddress = coinEvent.ToAddress,
                FromAddress = coinEvent.FromAddress,
                EventTime = coinEvent.EventTime,
                OperationId = coinEvent.OperationId
            };
        }
    }


    public class CoinEventRepository : ICoinEventRepository
    {
        private readonly INoSQLTableStorage<CoinEventEntity> _table;
        private readonly INoSQLTableStorage<AzureIndex> _index;
        private const string _operationIdIndex = "OperationIdIndex"; 

        public CoinEventRepository(INoSQLTableStorage<CoinEventEntity> table, INoSQLTableStorage<AzureIndex> index)
        {
            _table = table;
            _index = index;
        }

        public async Task<ICoinEvent> GetCoinEvent(string transactionHash)
        {
            var entity = await _table.GetDataAsync(CoinEventEntity.GetPartitionKey(), transactionHash);

            return entity;
        }

        public async Task<ICoinEvent> GetCoinEventById(string operationId)
        {
            var index = await _index.GetDataAsync(_operationIdIndex, operationId);
            if (index == null)
            {
                return null;
            }

            var entity = await _table.GetDataAsync(index);

            return entity;
        }

        public async Task InsertOrReplace(ICoinEvent coinEvent)
        {
            var entity = CoinEventEntity.CreateEntity(coinEvent);
            var index = new AzureIndex(_operationIdIndex, coinEvent.OperationId, entity);

            await _table.InsertOrReplaceAsync(entity);
            await _index.InsertOrReplaceAsync(index);
        }

        public async Task<IEnumerable<ICoinEvent>> GetAll()
        {
            var all = await _table.GetDataAsync(CoinEventEntity.GetPartitionKey());
            return all;
        }
    }
}

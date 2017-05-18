using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;

namespace AzureRepositories.Repositories
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
                EventTime = coinEvent.EventTime
            };
        }
    }


    public class CoinEventRepository : ICoinEventRepository
    {
        private readonly INoSQLTableStorage<CoinEventEntity> _table;

        public CoinEventRepository(INoSQLTableStorage<CoinEventEntity> table)
        {
            _table = table;
        }

        public async Task<ICoinEvent> GetCoinEvent(string transactionHash)
        {
            var entity = await _table.GetDataAsync(CoinEventEntity.GetPartitionKey(), transactionHash);

            return entity;
        }

        public async Task InsertOrReplace(ICoinEvent coinEvent)
        {
            var entity = CoinEventEntity.CreateEntity(coinEvent);

            await _table.InsertOrReplaceAsync(entity);
        }
    }
}

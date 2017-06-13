﻿using System;
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
    public class EventTraceEntity : TableEntity, IEventTrace
    {
        public static string GetPartitionKey(string operationId)
        {
            return $"EventTrace_{operationId}";
        }

        public string TimeKey { get { return this.RowKey; } set { this.RowKey = value; } }
        public string OperationId { get; set; }
        public DateTime TraceDate { get { return DateTime.Parse(TimeKey); } set { TimeKey = value.ToString(); } }
        public string Note { get; set; }

        public static EventTraceEntity CreateCoinEntity(IEventTrace trace)
        {
            return new EventTraceEntity
            {
                PartitionKey = GetPartitionKey(trace.OperationId),
                OperationId = trace.OperationId,
                TraceDate = trace.TraceDate,
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

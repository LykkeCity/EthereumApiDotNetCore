using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;

namespace AzureRepositories.Repositories
{
	public class MonitoringEntity : TableEntity, IMonitoring
	{
		private const string Key = "Monitoring";

		public DateTime DateTime { get; set; }
		public string ServiceName { get; set; }
		public string Version { get; set; }

		public static MonitoringEntity Create(IMonitoring monitoring)
		{
			return new MonitoringEntity
			{
				PartitionKey = Key,
				RowKey = monitoring.ServiceName,
				DateTime = monitoring.DateTime,
				Version = monitoring.Version
			};
		}
	}

	public class MonitoringRepository : IMonitoringRepository
	{
		private readonly INoSQLTableStorage<MonitoringEntity> _table;

		public MonitoringRepository(INoSQLTableStorage<MonitoringEntity> table)
		{
			_table = table;
		}

		public async Task SaveAsync(IMonitoring monitoring)
		{
			var entity = MonitoringEntity.Create(monitoring);
			await _table.InsertOrMergeAsync(entity);
		}

		public async Task<IEnumerable<IMonitoring>> GetList()
		{
			return await _table.GetDataAsync();
		}
	}
}

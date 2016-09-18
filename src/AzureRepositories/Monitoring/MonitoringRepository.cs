using System;
using System.Threading.Tasks;
using AzureRepositories.Azure;
using EthereumCore.Monitoring;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Monitoring
{
	public class MonitoringEntity : TableEntity, IMonitoring
	{
		private const string Key = "Monitoring";

		public DateTime DateTime { get; set; }
		public string ServiceName { get; set; }

		public static MonitoringEntity Create(IMonitoring monitoring)
		{
			return new MonitoringEntity
			{
				PartitionKey = Key,
				RowKey = monitoring.ServiceName,
				DateTime = monitoring.DateTime
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
	}
}

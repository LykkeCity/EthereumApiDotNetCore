using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
	public interface IMonitoring
	{
		DateTime DateTime { get; set; }
		string ServiceName { get; set; }
		string Version { get; set; }
	}

	public class Monitoring : IMonitoring
	{
		public DateTime DateTime { get; set; }
		public string ServiceName { get; set; }
		public string Version { get; set; }
	}

	public interface IMonitoringRepository
	{
		Task SaveAsync(IMonitoring redirect);
		Task<IEnumerable<IMonitoring>> GetList();
	}
}

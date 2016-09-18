using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EthereumCore.Monitoring
{
	public interface IMonitoring
	{
		DateTime DateTime { get; set; }
		string ServiceName { get; set; }
	}

	public class Monitoring : IMonitoring
	{
		public DateTime DateTime { get; set; }
		public string ServiceName { get; set; }
	}

	public interface IMonitoringRepository
	{
		Task SaveAsync(IMonitoring redirect);
	}
}

using System;
using Core.Settings;
using EthereumJobs.Job;
using Microsoft.Extensions.DependencyInjection;
using EthereumJobs.Config;

namespace EthereumJobs
{
	public class JobApp
	{
		public IServiceProvider Services { get; set; }

		public void Run(IBaseSettings settings)
		{
			IServiceCollection collection = new ServiceCollection();
			collection.InitJobDependencies(settings);

			Services = collection.BuildServiceProvider();

			RunJobs();
		}

		public void RunJobs()
		{
			Services.GetService<CheckContractQueueCountJob>().Start();
			Services.GetService<CheckPaymentsToUserContractsJob>().Start();
			Services.GetService<RefreshContractQueueJob>().Start();
			Services.GetService<MonitoringJob>().Start();
		}
	}
}

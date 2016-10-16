using System;
using System.Threading.Tasks;
using Core.Settings;
using EthereumJobs.Actions;
using EthereumJobs.Job;
using Microsoft.Extensions.DependencyInjection;
using EthereumJobs.Config;

namespace EthereumJobs
{
	public class JobApp
	{
		public IServiceProvider Services { get; set; }

		public async void Run(IBaseSettings settings)
		{
			IServiceCollection collection = new ServiceCollection();
			collection.InitJobDependencies(settings);

			Services = collection.BuildServiceProvider();

			// start monitoring
			Services.GetService<MonitoringJob>().Start();

			// restore contract payment events after service shutdown
			await Task.Run(() => Services.GetService<ProcessManualEvents>().Start());
			await Task.Run(() => Services.GetService<CatchOldUserContractEvents>().Start());

			Console.WriteLine($"----------- All data checked and restored, job is running now {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}-----------");

			RunJobs();
		}

		public void RunJobs()
		{
			Services.GetService<CheckContractQueueCountJob>().Start();
			Services.GetService<CheckPaymentsToUserContractsJob>().Start();
			Services.GetService<RefreshContractQueueJob>().Start();
			Services.GetService<TransferTransactionQueueJob>().Start();
			Services.GetService<MonitoringContractBalance>().Start();

			Services.GetService<ListenCoinContactsEvents>().Start();
			Services.GetService<MonitoringCoinTransactionJob>().Start();
			Services.GetService<PingContractsJob>().Start();

		}
	}
}

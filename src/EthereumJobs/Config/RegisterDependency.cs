using Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Services;
using AzureRepositories;
using EthereumJobs.Actions;
using EthereumJobs.Job;

namespace EthereumJobs.Config
{
	public static class RegisterDepencency
	{
		public static void InitJobDependencies(this IServiceCollection collection, IBaseSettings settings)
		{
			collection.AddSingleton(settings);

			collection.RegisterAzureLogs(settings, "Job");
			collection.RegisterAzureStorages(settings);
			collection.RegisterAzureQueues(settings);

			collection.RegisterServices();

			RegisterJobs(collection);
		}

		private static void RegisterJobs(IServiceCollection collection)
		{
			collection.AddTransient<CatchOldUserContractEvents>();
			collection.AddTransient<ProcessManualEvents>();

			collection.AddSingleton<CheckContractQueueCountJob>();
			collection.AddSingleton<CheckPaymentsToUserContractsJob>();
			collection.AddSingleton<RefreshContractQueueJob>();
			collection.AddSingleton<MonitoringJob>();
			collection.AddSingleton<TransferTransactionQueueJob>();
			collection.AddSingleton<MonitoringContractBalance>();

			collection.AddSingleton<ListenCoinContactsEvents>();
			collection.AddSingleton<MonitoringCoinTransactionJob>();
			collection.AddSingleton<PingContractsJob>();
		}
	}
}

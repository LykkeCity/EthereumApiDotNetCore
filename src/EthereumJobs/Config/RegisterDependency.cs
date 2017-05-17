using Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Services;
using AzureRepositories;
using EthereumJobs.Job;
using Lykke.JobTriggers.Abstractions;
using AzureRepositories.Notifiers;
using Common.Log;
using RabbitMQ;

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
            var provider = collection.BuildServiceProvider();

            collection.RegisterRabbitQueue(settings, provider.GetService<ILog>());
            collection.AddTransient<IPoisionQueueNotifier, SlackNotifier>();
            RegisterJobs(collection);
        }

        private static void RegisterJobs(IServiceCollection collection)
        {
            collection.AddSingleton<MonitoringJob>();

            #region NewJobs

            collection.AddSingleton<MonitoringCoinTransactionJob>();
            collection.AddSingleton<MonitoringTransferContracts>();
            collection.AddSingleton<MonitoringTransferTransactions>();
            collection.AddSingleton<TransferContractPoolJob>();
            collection.AddSingleton<TransferContractUserAssignmentJob>();
            collection.AddSingleton<PoolRenewJob>();
            collection.AddSingleton<PingContractsJob>();
            collection.AddSingleton<TransferTransactionQueueJob>();

            #endregion

        }
    }
}

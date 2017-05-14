using Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Services;
using AzureRepositories;
using EthereumJobs.Actions;
using EthereumJobs.Job;
using Lykke.JobTriggers.Abstractions;
using AzureRepositories.Notifiers;

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
            collection.AddTransient<IPoisionQueueNotifier, SlackNotifier>();
            RegisterJobs(collection);
        }

        private static void RegisterJobs(IServiceCollection collection)
        {
            collection.AddTransient<ProcessManualEvents>();
            collection.AddSingleton<MonitoringJob>();
            collection.AddSingleton<TransferTransactionQueueJob>();
            collection.AddSingleton<MonitoringCoinTransactionJob>();

            #region NewJobs

            collection.AddSingleton<MonitoringTransferContracts>();
            collection.AddSingleton<MonitoringTransferTransactions>();
            collection.AddSingleton<TransferContractPoolJob>();
            collection.AddSingleton<TransferContractUserAssignmentJob>();

            #endregion

        }
    }
}

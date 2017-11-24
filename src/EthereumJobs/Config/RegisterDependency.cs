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
    public static class RegisterDependency
    {
        public static void InitJobDependencies(this IServiceCollection collection, IBaseSettings settings, ISlackNotificationSettings slackNotificationSettings)
        {
            collection.AddSingleton(settings);

            collection.RegisterAzureLogs(settings, "Job");
            collection.RegisterAzureStorages(settings, slackNotificationSettings);
            collection.RegisterAzureQueues(settings, slackNotificationSettings);

            collection.RegisterServices();
            var provider = collection.BuildServiceProvider();

            collection.RegisterRabbitQueue(settings, provider.GetService<ILog>());
            collection.AddTransient<IPoisionQueueNotifier, SlackNotifier>();
            collection.AddSingleton(new Lykke.MonitoringServiceApiCaller.MonitoringServiceFacade(settings.MonitoringServiceUrl));
            RegisterJobs(collection);
        }

        public static void RegisterJobs(IServiceCollection collection)
        {
            #region NewJobs

            collection.AddSingleton<MonitoringJob>();
            collection.AddSingleton<MonitoringCoinTransactionJob>();
            collection.AddSingleton<MonitoringTransferContracts>();
            collection.AddSingleton<MonitoringTransferTransactions>();
            collection.AddSingleton<TransferContractPoolJob>();
            collection.AddSingleton<TransferContractUserAssignmentJob>();
            collection.AddSingleton<PoolRenewJob>();
            collection.AddSingleton<PingContractsJob>();
            collection.AddSingleton<TransferTransactionQueueJob>();
            collection.AddSingleton<MonitoringOperationJob>();
            collection.AddSingleton<CashinIndexingJob>();
            collection.AddSingleton<CoinEventResubmittJob>();
            collection.AddSingleton<HotWalletCashoutJob>();
            collection.AddSingleton<HotWalletMonitoringTransactionJob>();
            collection.AddSingleton<Erc20DepositContractPoolJob>();
            collection.AddSingleton<Erc20DepositContractPoolRenewJob>();
            collection.AddSingleton<Erc20DepositMonitoringCashinTransactions>();
            collection.AddSingleton<Erc20DepositMonitoringContracts>();

            #endregion
        }
    }
}

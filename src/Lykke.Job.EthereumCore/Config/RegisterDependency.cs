using Lykke.Service.EthereumCore.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.EthereumCore.AzureRepositories;
using Lykke.Job.EthereumCore.Job;
using Lykke.JobTriggers.Abstractions;
using Lykke.Service.EthereumCore.AzureRepositories.Notifiers;
using Common.Log;
using RabbitMQ;
using Lykke.Service.RabbitMQ;
using Lykke.SettingsReader;

namespace Lykke.Job.EthereumCore.Config
{
    public static class RegisterDependency
    {
        public static void InitJobDependencies(this IServiceCollection collection, 
            IReloadingManager<BaseSettings> settings, 
            IReloadingManager<SlackNotificationSettings> slackNotificationSettings,
            ILog log)
        {
            collection.AddSingleton(settings);

            collection.RegisterAzureStorages(settings, slackNotificationSettings);
            collection.RegisterAzureQueues(settings, slackNotificationSettings);
            collection.RegisterServices();
            collection.RegisterRabbitQueue(settings, log);
            collection.AddTransient<IPoisionQueueNotifier, SlackNotifier>();
            collection.AddSingleton(new Lykke.MonitoringServiceApiCaller.MonitoringServiceFacade(settings.CurrentValue.MonitoringServiceUrl));
            RegisterJobs(collection);
        }

        public static void RegisterJobs(IServiceCollection collection)
        {
            #region NewJobs

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

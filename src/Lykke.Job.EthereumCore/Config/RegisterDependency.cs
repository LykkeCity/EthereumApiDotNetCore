using Autofac;
using Autofac.Features.AttributeFilters;
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
            ContainerBuilder builder,
            IReloadingManager<BaseSettings> settings, 
            IReloadingManager<SlackNotificationSettings> slackNotificationSettings,
            ILog log)
        {
            collection.AddSingleton(settings);

            builder.RegisterAzureStorages(settings, slackNotificationSettings, log);
            builder.RegisterAzureQueues(settings, slackNotificationSettings);
            collection.RegisterServices();
            builder.RegisterServices();
            collection.RegisterRabbitQueue(settings, log);
            collection.AddTransient<IPoisionQueueNotifier, SlackNotifier>();
            collection.AddSingleton(new Lykke.MonitoringServiceApiCaller.MonitoringServiceFacade(settings.CurrentValue.MonitoringServiceUrl));
            RegisterJobs(builder);
        }

        public static void RegisterJobs(ContainerBuilder builder)
        {
            #region NewJobs

            //builder.RegisterType<<MonitoringCoinTransactionJob>();
            //builder.RegisterType<<MonitoringTransferContracts>();
            //builder.RegisterType<<MonitoringTransferTransactions>();
            //builder.RegisterType<<TransferContractPoolJob>();
            //builder.RegisterType<<TransferContractUserAssignmentJob>();
            //builder.RegisterType<<PoolRenewJob>();
            //builder.RegisterType<<PingContractsJob>();
            //builder.RegisterType<<TransferTransactionQueueJob>();
            //builder.RegisterType<<MonitoringOperationJob>();
            //builder.RegisterType<<CashinIndexingJob>();
            //builder.RegisterType<<CoinEventResubmittJob>();
            //builder.RegisterType<<HotWalletCashoutJob>();
            //builder.RegisterType<<HotWalletMonitoringTransactionJob>();
            builder.RegisterType<Erc20DepositContractPoolJob>()
                .SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<<Erc20DepositContractPoolRenewJob>();
            //builder.RegisterType<<Erc20DepositMonitoringCashinTransactions>();
            //builder.RegisterType<<Erc20DepositMonitoringContracts>();

            #region LykkePay

            //builder.RegisterType<<MonitoringCoinTransactionJob>();

            #endregion

            #endregion
        }
    }
}

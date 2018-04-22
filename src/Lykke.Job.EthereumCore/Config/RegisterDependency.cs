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
using Lykke.Job.EthereumCore.Job.LykkePay;
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

            //builder.RegisterType<MonitoringCoinTransactionJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<MonitoringTransferContracts>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<MonitoringTransferTransactions>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<TransferContractPoolJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<TransferContractUserAssignmentJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<PoolRenewJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<PingContractsJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<TransferTransactionQueueJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<MonitoringOperationJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<CashinIndexingJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<CoinEventResubmittJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<HotWalletCashoutJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<HotWalletMonitoringTransactionJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<Erc20DepositContractPoolJob>()
            //    .SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<Erc20DepositContractPoolRenewJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<Erc20DepositMonitoringCashinTransactions>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<Erc20DepositMonitoringContracts>().SingleInstance().WithAttributeFiltering();

            #region LykkePay

            //builder.RegisterType<LykkePayErc20DepositTransferStarterJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<LykkePayHotWalletMonitoringTransactionJob>().SingleInstance().WithAttributeFiltering();
            //TODO:Test on icoming transfers
            //builder.RegisterType<LykkePayIndexingJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<LykkePayTransferNotificationJob>().SingleInstance().WithAttributeFiltering();
            #endregion

            #endregion
        }
    }
}

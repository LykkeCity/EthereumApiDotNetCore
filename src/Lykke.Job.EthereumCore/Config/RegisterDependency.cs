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
using Lykke.Job.EthereumCore.Job.Airlines;
using Lykke.Job.EthereumCore.Job.LykkePay;
using Lykke.Service.AirlinesJobRunner.Job;
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
            builder.RegisterAzureQueues(settings.Nested(x => x.Db.DataConnString), slackNotificationSettings);
            collection.RegisterServices();
            builder.RegisterServices();
            collection.RegisterRabbitQueue(settings.Nested(x => x.RabbitMq),
                settings.Nested(x => x.Db.DataConnString),
                log);
            collection.AddTransient<IPoisionQueueNotifier, SlackNotifier>();
            collection.AddSingleton(new Lykke.MonitoringServiceApiCaller.MonitoringServiceFacade(settings.CurrentValue.MonitoringServiceUrl));
            RegisterJobs(builder, settings);
        }

        public static void RegisterJobs(ContainerBuilder builder, IReloadingManager<BaseSettings> settings)
        {
            #region NewJobs

            builder.RegisterType<MonitoringCoinTransactionJob>().SingleInstance().WithAttributeFiltering();
            //Stop monitoring cashins
            //builder.RegisterType<MonitoringTransferContracts>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<MonitoringTransferTransactions>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransferContractPoolJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransferContractUserAssignmentJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<PoolRenewJob>().SingleInstance().WithAttributeFiltering();
            //builder.RegisterType<PingContractsJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransferTransactionQueueJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<MonitoringOperationJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<CashinIndexingJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<CoinEventResubmittJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<HotWalletCashoutJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<HotWalletMonitoringTransactionJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<Erc20DepositContractPoolJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<Erc20DepositContractPoolRenewJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<Erc20DepositMonitoringCashinTransactions>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<Erc20DepositMonitoringContracts>()
                .SingleInstance()
                .WithAttributeFiltering()
                .WithParameter(TypedParameter.From(settings.CurrentValue.BlockPassTokenAddress));

            #region LykkePay

            builder.RegisterType<LykkePayErc20DepositTransferStarterJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<LykkePayHotWalletMonitoringTransactionJob>().SingleInstance().WithAttributeFiltering();
            //TODO:Test on icoming transfers
            builder.RegisterType<LykkePayIndexingJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<LykkePayTransferNotificationJob>().SingleInstance().WithAttributeFiltering();
            #endregion

            #region Airlines

            builder.RegisterType<Erc20DepositTransferStarterJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<AirlinesHotWalletMonitoringTransactionJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransferNotificationJob>().SingleInstance().WithAttributeFiltering();

            #endregion

            #endregion
        }
    }
}

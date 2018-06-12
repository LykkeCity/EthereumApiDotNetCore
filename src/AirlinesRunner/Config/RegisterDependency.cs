using Autofac;
using Autofac.Features.AttributeFilters;
using Common.Log;
using Lykke.JobTriggers.Abstractions;
using Lykke.Service.AirlinesJobRunner.Job;
using Lykke.Service.EthereumCore.AzureRepositories;
using Lykke.Service.EthereumCore.AzureRepositories.Notifiers;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.RabbitMQ;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.AirlinesJobRunner
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

        public static void InitAirLinesDependencies(ContainerBuilder builder)
        {
            #region Airlines

            builder.RegisterType<Erc20DepositTransferStarterJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<HotWalletMonitoringTransactionJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransferNotificationJob>().SingleInstance().WithAttributeFiltering();

            #endregion
        }

        public static void RegisterJobs(ContainerBuilder builder)
        {
            #region Airlines

            builder.RegisterType<Erc20DepositTransferStarterJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<HotWalletMonitoringTransactionJob>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransferNotificationJob>().SingleInstance().WithAttributeFiltering();

            #endregion
        }
    }
}

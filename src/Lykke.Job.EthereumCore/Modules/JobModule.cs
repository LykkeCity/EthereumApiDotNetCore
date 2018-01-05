using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.SettingsReader;
using Lykke.JobTriggers.Extenstions;
using AzureStorage.Blob;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.EthereumCore.Core.Services;
using System;

namespace Lykke.Job.EthereumCore.Modules
{
    public class JobModule : Module
    {
        private readonly IReloadingManager<AppSettings> _dbSettingsManager;
        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public JobModule(IReloadingManager<AppSettings> dbSettingsManager, ILog log)
        {
            _log = log;
            _dbSettingsManager = dbSettingsManager;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            // NOTE: Do not register entire settings in container, pass necessary settings to services which requires them
            // ex:
            // builder.RegisterType<QuotesPublisher>()
            //  .As<IQuotesPublisher>()
            //  .WithParameter(TypedParameter.From(_settings.Rabbit.ConnectionString))

            Lykke.Job.EthereumCore.Config.RegisterDependency.InitJobDependencies(_services,
                    _dbSettingsManager.Nested(x => x.EthereumCore),
                    _dbSettingsManager.Nested(x => x.SlackNotifications),
                    _log);
            _services.AddSingleton(_dbSettingsManager.CurrentValue);
            _services.AddTriggers(pool =>
            {
                // default connection must be initialized
                pool.AddDefaultConnection(_dbSettingsManager.CurrentValue.EthereumCore.Db.DataConnString);
            });

            Console.WriteLine($"----------- Job is running now {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}-----------");

            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();

            RegisterAzureQueueHandlers(builder);

            // TODO: Add your dependencies here

            builder.Populate(_services);
        }

        private void RegisterAzureQueueHandlers(ContainerBuilder builder)
        {
            // NOTE: You can implement your own poison queue notifier for azure queue subscription.
            // See https://github.com/LykkeCity/JobTriggers/blob/master/readme.md
            // builder.Register<PoisionQueueNotifierImplementation>().As<IPoisionQueueNotifier>();

            builder.AddTriggers(
                pool =>
                {
                    pool.AddDefaultConnection(_dbSettingsManager.CurrentValue.EthereumCore.Db.DataConnString);
                });
        }
    }
}

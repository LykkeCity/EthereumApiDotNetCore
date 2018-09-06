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
        private readonly IReloadingManager<AppSettings> _settings;
        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public JobModule(IReloadingManager<AppSettings> dbSettingsManager, ILog log)
        {
            _log = log;
            _settings = dbSettingsManager;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            var nesetdBaseSettings = _settings.Nested(x => x.EthereumCore);
            var nesetdSlackSettings = _settings.Nested(x => x.SlackNotifications);
            _services.AddSingleton<IBaseSettings>(_settings.CurrentValue.EthereumCore);
            _services.AddSingleton(_settings.CurrentValue);
            _services.AddSingleton(_settings);
            _services.AddSingleton(_settings.CurrentValue.BlockPassClient);
            _services.AddSingleton(_settings.Nested(X => X.EthereumCore));
            _services.AddSingleton(_settings.Nested(X => X.BlockPassClient));

            Lykke.Job.EthereumCore.Config.RegisterDependency.InitJobDependencies(_services,
                     builder,
                    _settings.Nested(x => x.EthereumCore),
                    _settings.Nested(x => x.SlackNotifications),
                    _log);
            _services.AddSingleton(_settings.CurrentValue);
           
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
                    pool.AddDefaultConnection(_settings.ConnectionString(x => x.EthereumCore.Db.DataConnString));
                });
        }
    }
}

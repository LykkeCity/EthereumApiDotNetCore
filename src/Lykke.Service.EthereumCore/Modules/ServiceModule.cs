using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Service.EthereumCore.AzureRepositories;
using Lykke.Service.EthereumCore.Core.Services;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Core.Utils;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.RabbitMQ;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.EthereumCore.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;
        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public ServiceModule(IReloadingManager<AppSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;

            _services = new ServiceCollection();
        }

        public ServiceProvider ServiceProvider { get; private set; }

        protected override void Load(ContainerBuilder builder)
        {
            if (_settings.CurrentValue.ChaosKitty != null)
            {
                //For the dark gods
                ChaosKitty.StateOfChaos = _settings.CurrentValue.ChaosKitty.StateOfChaos;
            }
            var nesetdBaseSettings = _settings.Nested(x => x.EthereumCore);
            var nesetdSlackSettings = _settings.Nested(x => x.SlackNotifications);
            _services.AddSingleton<IBaseSettings>(_settings.CurrentValue.EthereumCore);
            _services.AddSingleton(_settings.CurrentValue);
            _services.AddSingleton(_settings);
            _services.AddSingleton(_settings.Nested(X => X.EthereumCore));
            //builder.RegisterAzureLogs(settings.EthereumCore, "Api");
            builder.RegisterAzureStorages(nesetdBaseSettings, nesetdSlackSettings, _log);
            builder.RegisterAzureQueues(nesetdBaseSettings, nesetdSlackSettings);
            _services.RegisterServices();

            ServiceProvider = _services.BuildServiceProvider();
            _services.RegisterRabbitQueue(nesetdBaseSettings, _log);

            #region Services

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

            #endregion

            builder.Populate(_services);
        }
    }
}

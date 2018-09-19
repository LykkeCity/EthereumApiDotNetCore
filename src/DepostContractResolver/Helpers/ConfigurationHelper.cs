using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Service.EthereumCore.AzureRepositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.RabbitMQ;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace DepositContractResolver.Helpers
{
    public class ConfigurationHelper : IConfigurationHelper
    {
        public (IContainer resolver, ILog logToConsole) GetResolver(IReloadingManager<AppSettings> appSettings)
        {
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IServiceCollection collection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            containerBuilder.RegisterInstance(appSettings);
            containerBuilder.RegisterInstance<IBaseSettings>(appSettings.CurrentValue.EthereumCore);
            containerBuilder.RegisterInstance<ISlackNotificationSettings>(appSettings.CurrentValue.SlackNotifications);
            containerBuilder.RegisterInstance(appSettings.Nested(x => x.EthereumCore));
            containerBuilder.RegisterInstance(appSettings.CurrentValue);
            var consoleLogger = new LogToConsole();
            collection.AddSingleton<ILog>(consoleLogger);
            RegisterReposExt.RegisterAzureQueues(containerBuilder, appSettings.Nested(x => x.EthereumCore.Db.DataConnString),
                appSettings.Nested(x => x.SlackNotifications));
            RegisterReposExt.RegisterAzureStorages(containerBuilder, appSettings.Nested(x => x.EthereumCore),
                appSettings.Nested(x => x.SlackNotifications), consoleLogger);
            RegisterRabbitQueueEx.RegisterRabbitQueue(collection,
                appSettings.Nested(x => x.EthereumCore.RabbitMq),
                appSettings.Nested(x => x.EthereumCore.Db.DataConnString),
                consoleLogger);
            RegisterDependency.RegisterServices(collection);
            RegisterDependency.RegisterServices(containerBuilder);
            containerBuilder.Populate(collection);
            containerBuilder.RegisterInstance<ILog>(consoleLogger);
            var resolver = containerBuilder.Build();
            resolver.ActivateRequestInterceptor();

            return (resolver, consoleLogger);
        }

        public IReloadingManager<AppSettings> GetCurrentSettingsFromUrl(string settingsUrl)
        {
            var keyValuePair = new KeyValuePair<string, string>[1]
            {
                new KeyValuePair<string, string>("SettingsUrl", settingsUrl)
            };

            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            builder.AddInMemoryCollection(keyValuePair);
            var configuration = builder.Build();
            var settings = configuration.LoadSettings<AppSettings>();

            return settings;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Service.EthereumCore.AzureRepositories;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.EthereumCore.Services.Coins;
using Lykke.Service.RabbitMQ;
using Lykke.SettingsReader;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;

namespace DepostContractResolver
{
    class Program
    {
        static void Main(string[] args)
        {
            Microsoft.Extensions.CommandLineUtils.CommandLineApplication application = new CommandLineApplication(true);

            var settingsUrlArg = application.Argument("SettingsUrl", "ethereum core SettingsUrl", false);
            var fromAddressArg = 
                application.Argument("FromAddress", "FromAddress is an address of user old(legacy) deposit address", false);
            var toAddressArg = 
                application.Argument("ToAddress", "ToAddress is the user's address of ETH asset in blockchain integration layer", false);

            application.OnExecute(async () => 
                await TransferFromLegacyDepositToUsersEthDepositAsync(
                    settingsUrlArg.Value,
                    fromAddressArg.Value,
                    toAddressArg.Value));
            application.Execute(args);
        }

        static async Task<int> TransferFromLegacyDepositToUsersEthDepositAsync(
            string settingsUrl,
            string fromAddress,
            string toAddress)
        {
            #region RegisterDependencies

            var appSettings = GetCurrentSettingsFromUrl(settingsUrl);
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
            var resolver = containerBuilder.Build();

            #endregion

            string depositContractAddress = fromAddress?.ToLower();
            var transferContractRepository = resolver.Resolve<ITransferContractRepository>();
            var transferContractService = resolver.Resolve<ITransferContractService>();
            var contractService = resolver.Resolve<IContractService>();
            var exchangeContractService = resolver.Resolve<IExchangeContractService>();
            var oldDeposiTransferContract = await transferContractRepository.GetAsync(depositContractAddress);
            BigInteger balance = await transferContractService.GetBalance(depositContractAddress); //ETH Balance in wei

            var transactionHash = await transferContractService.RecievePaymentFromTransferContract(
                oldDeposiTransferContract.ContractAddress, 
                oldDeposiTransferContract.CoinAdapterAddress);
            await contractService.WaitForTransactionToCompleteAsync(transactionHash);
            var guid = Guid.NewGuid();
            await exchangeContractService.TransferWithoutSignCheck(guid,
                oldDeposiTransferContract.CoinAdapterAddress,
                oldDeposiTransferContract.ContractAddress,
                toAddress, 
                balance, 
                "");


            return 0;
        }

        static IReloadingManager<AppSettings> GetCurrentSettingsFromUrl(string settingsUrl)
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

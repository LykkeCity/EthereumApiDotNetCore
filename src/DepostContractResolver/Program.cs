using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
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

namespace DepositContractResolver
{
    class Program
    {
        static void Main(string[] args)
        {
            Microsoft.Extensions.CommandLineUtils.CommandLineApplication application = new CommandLineApplication(true);
            application.Name = "DepositContractResolver";
            application.Description = ".NET Core console app to retrieve funds from deposit address and legacy coin adapter.";

            application.Command("transfer-from-deposit", lineApplication =>
            {
                lineApplication.Description = "This is the description for transfer-from-deposit.";
                lineApplication.HelpOption("-?|-h|--help");

                var settingsUrlOption = application.Option("-s|--settings <optionvalue>", 
                    "ethereum core SettingsUrl", 
                    CommandOptionType.SingleValue);
                var fromAddressOption =
                    application.Option("-f|--from-address <optionvalue>", 
                        "FromAddress is an address of user old(legacy) deposit address", 
                        CommandOptionType.SingleValue);
                var toAddressOption =
                    application.Option("-t|--to-address <optionvalue>",
                        "ToAddress is the user's address of ETH asset in blockchain integration layer",
                        CommandOptionType.SingleValue);

                lineApplication.OnExecute(async () =>
                    await TransferFromLegacyDepositToUsersEthDepositAsync(
                        settingsUrlOption.Value(),
                        fromAddressOption.Value(),
                          toAddressOption.Value()));
            }, true);

            application.Command("transfer-from-adapter", lineApplication =>
            {
                lineApplication.Description = "This is the description for transfer-from-adapter.";
                lineApplication.HelpOption("-?|-h|--help");

                var settingsUrlOption = application.Option("-s|--settings <optionvalue>",
                    "ethereum core SettingsUrl",
                    CommandOptionType.SingleValue);
                var fromAddressOption =
                    application.Option("-f|--from-address <optionvalue>",
                        "FromAddress is an address of user old(legacy) deposit address",
                        CommandOptionType.SingleValue);
                var toAddressOption =
                    application.Option("-t|--to-address <optionvalue>",
                        "ToAddress is the user's address of ETH asset in blockchain integration layer",
                        CommandOptionType.SingleValue);

                lineApplication.OnExecute(async () =>
                    await TransferFromLegacyDepositToUsersEthDepositAsync(
                        settingsUrlOption.Value(),
                        fromAddressOption.Value(),
                        toAddressOption.Value()));
            }, true);

            application.Execute(args);
        }

        static async Task<int> TransferFromLegacyDepositToUsersEthDepositAsync(
            string settingsUrl,
            string fromAddress,
            string toAddress)
        {
            #region RegisterDependencies

            var appSettings = GetCurrentSettingsFromUrl(settingsUrl);
            var (resolver, consoleLogger) = GetResolver(appSettings);

            #endregion

            string depositContractAddress = fromAddress?.ToLower();
            var transferContractRepository = resolver.Resolve<ITransferContractRepository>();
            var transferContractService = resolver.Resolve<ITransferContractService>();
            var contractService = resolver.Resolve<IContractService>();
            var exchangeContractService = resolver.Resolve<IExchangeContractService>();
            var oldDeposiTransferContract = await transferContractRepository.GetAsync(depositContractAddress);

            if (oldDeposiTransferContract == null)
            {
                await consoleLogger.WriteInfoAsync(nameof(TransferFromLegacyDepositToUsersEthDepositAsync),
                    depositContractAddress, "Deposit contract does not exist");

                return 0;
            }

            BigInteger balance = await transferContractService.GetBalance(depositContractAddress); //ETH Balance in wei

            if (balance == 0)
            {
                await consoleLogger.WriteInfoAsync(nameof(TransferFromLegacyDepositToUsersEthDepositAsync),
                    depositContractAddress, "Deposit contract balance is 0");

                return 0;
            }

            var transactionHash = await transferContractService.RecievePaymentFromTransferContract(
                oldDeposiTransferContract.ContractAddress,
                oldDeposiTransferContract.CoinAdapterAddress);

            await consoleLogger.WriteInfoAsync(nameof(TransferFromLegacyDepositToUsersEthDepositAsync),
                depositContractAddress, $"Transfer from deposit address to adapter is pending. {transactionHash}");

            await contractService.WaitForTransactionToCompleteAsync(transactionHash);

            BigInteger adapterBalance = await transferContractService.GetBalanceOnAdapter(
                oldDeposiTransferContract.CoinAdapterAddress,
                oldDeposiTransferContract.UserAddress,
                checkInPendingBlock: true);

            var guid = Guid.NewGuid();
            string transactionHashFromAdapter = await exchangeContractService.CashOutWithoutSignCheck(guid,
                oldDeposiTransferContract.CoinAdapterAddress,
                oldDeposiTransferContract.UserAddress,
                toAddress,
                balance);

            await consoleLogger.WriteInfoAsync(nameof(TransferFromLegacyDepositToUsersEthDepositAsync),
                depositContractAddress, $"Transfer from the adapter address to the destination is pending. {transactionHashFromAdapter}");

            await contractService.WaitForTransactionToCompleteAsync(transactionHashFromAdapter);

            await consoleLogger.WriteInfoAsync(nameof(TransferFromLegacyDepositToUsersEthDepositAsync),
                depositContractAddress, $"Transfer to the destination address is completed. check:" +
                                        $"https://etherscan.io/tx/{transactionHashFromAdapter}");

            return 0;
        }

        static async Task<int> TransferFromAdapterToUsersEthDepositAsync(
            string settingsUrl,
            string coinAdapter,
            string fromAddress,
            string toAddress)
        {
            #region RegisterDependencies

            var appSettings = GetCurrentSettingsFromUrl(settingsUrl);
            var (resolver, consoleLogger) = GetResolver(appSettings);

            #endregion

            string depositContractAddress = fromAddress?.ToLower();
            var transferContractRepository = resolver.Resolve<ITransferContractRepository>();
            var transferContractService = resolver.Resolve<ITransferContractService>();
            var contractService = resolver.Resolve<IContractService>();
            var exchangeContractService = resolver.Resolve<IExchangeContractService>();
            var oldDeposiTransferContract = await transferContractRepository.GetAsync(depositContractAddress);

            BigInteger adapterBalance = await transferContractService.GetBalanceOnAdapter(
                oldDeposiTransferContract.CoinAdapterAddress,
                oldDeposiTransferContract.UserAddress,
                checkInPendingBlock: true);

            if (adapterBalance == 0)
            {
                await consoleLogger.WriteInfoAsync(nameof(TransferFromAdapterToUsersEthDepositAsync),
                    depositContractAddress, "Adapter balance is 0");

                return 0;
            }

            var guid = Guid.NewGuid();
            string transactionHashFromAdapter = await exchangeContractService.CashOutWithoutSignCheck(guid,
                coinAdapter,
                fromAddress,
                toAddress,
                adapterBalance);

            await consoleLogger.WriteInfoAsync(nameof(TransferFromAdapterToUsersEthDepositAsync),
                depositContractAddress, $"Transfer from the adapter address to the destination is pending. {transactionHashFromAdapter}");

            await contractService.WaitForTransactionToCompleteAsync(transactionHashFromAdapter);

            await consoleLogger.WriteInfoAsync(nameof(TransferFromAdapterToUsersEthDepositAsync),
                depositContractAddress, $"Transfer to the destination address is completed. check:" +
                                        $"https://etherscan.io/tx/{transactionHashFromAdapter}");

            return 0;
        }

        private static (IContainer resolver, ILog logToConsole) GetResolver(IReloadingManager<AppSettings> appSettings)
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

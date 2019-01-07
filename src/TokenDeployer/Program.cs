using Autofac;
using Common.Log;
using Lykke.Service.EthereumCore.AzureRepositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.RabbitMQ;
using Lykke.SettingsReader;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Common;
using Lykke.Service.EthereumCore.Core.Services;
using Lykke.Service.EthereumCore.Services.Coins;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;

namespace TokenDeployer
{
    class Program
    {
        static void Main(string[] args)
        {
            Microsoft.Extensions.CommandLineUtils.CommandLineApplication application = new CommandLineApplication(true);

            var settingsUrlArg = application.Argument("SettingsUrl", "Ethereum Core SettingsUrl", false);
            var pathToTokenConfigArg =
                application.Argument("PathToTokenConfig", "Path to file with token settings", false);

            application.OnExecute(async () =>
                await DeployTokenAsync(
                    settingsUrlArg.Value,
                    pathToTokenConfigArg.Value));
            application.Execute(args);
        }

        static async Task<int> DeployTokenAsync(
            string settingsUrl,
            string tokenCfgPath)
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
            containerBuilder.Populate(collection);
            containerBuilder.RegisterInstance<ILog>(consoleLogger);
            var resolver = containerBuilder.Build();
            resolver.ActivateRequestInterceptor();
            #endregion

            var web3 = resolver.Resolve<IWeb3>();
            var contractService = resolver.Resolve<IContractService>();
            var ercInterfaceService = resolver.Resolve<IErcInterfaceService>();
            var exchangeContractService = resolver.Resolve<IExchangeContractService>();
            var text = File.ReadAllText(tokenCfgPath);
            var tokenCfg = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenCfg>(text);
            var addressUtil = new AddressUtil();

            if (!exchangeContractService.IsValidAddress(tokenCfg.HotwalletAddress))
            {
                await consoleLogger.WriteInfoAsync(nameof(Main),
                    tokenCfg.ToJson(),
                    $"HotwalletAddress is not a valid address.");

                return 0;
            }

            await consoleLogger.WriteInfoAsync(nameof(Main), "", $"Started Deployment");

            foreach (var tokenDescr in tokenCfg.Tokens)
            {
                await consoleLogger.WriteInfoAsync(nameof(Main), "", $"Processing {tokenDescr.TokenName}");

                if (!BigInteger.TryParse(tokenDescr.InitialSupply, out var initialSupply) || initialSupply == 0)
                {
                    await consoleLogger.WriteInfoAsync(nameof(Main),
                        tokenDescr.ToJson(),
                        $"Can't parse initial supply value. It is not a BigInt or zero");

                    continue;
                }

                if (!exchangeContractService.IsValidAddress(tokenDescr.IssuerAddress))
                {
                    await consoleLogger.WriteInfoAsync(nameof(Main),
                        tokenDescr.ToJson(),
                        $"Issuer address is not a valid address.");

                    continue;
                }

                var (abi,bytecode) = GetContractDeploymentForTokenType(tokenDescr.TokenType);
                string address = tokenDescr.TokenType != TokenType.NonEmissive ? 
                    await contractService.CreateContract(abi,
                                            bytecode,
                                            4000000,
                                            tokenDescr.IssuerAddress,
                                            tokenDescr.TokenName,
                                            tokenDescr.Divisibility,
                                            tokenDescr.TokenSymbol,
                                            tokenDescr.Version) : 
                    await contractService.CreateContract(abi,
                        bytecode,
                        4000000,
                        tokenDescr.IssuerAddress,
                        tokenDescr.TokenName,
                        tokenDescr.Divisibility,
                        tokenDescr.TokenSymbol,
                        tokenDescr.Version,
                        initialSupply);

                await consoleLogger.WriteInfoAsync(nameof(Main), tokenDescr.ToJson(), $"Deployed at address {address}");
            }

            foreach (var transfer in tokenCfg.Transfers)
            {
                if (!BigInteger.TryParse(transfer.Amount, out var amount) || amount == 0)
                {
                    await consoleLogger.WriteInfoAsync(nameof(Main),
                        transfer.ToJson(),
                        $"Can't parse amount value. It is not a BigInt or zero");

                    continue;
                }

                if (!exchangeContractService.IsValidAddress(transfer.IssuerAddress))
                {
                    await consoleLogger.WriteInfoAsync(nameof(Main),
                        transfer.ToJson(),
                        $"Issuer address is not a valid address.");

                    continue;
                }

                await consoleLogger.WriteInfoAsync(nameof(Main), transfer.ToJson(),
                    $"Starting Emission to {tokenCfg.HotwalletAddress}");
                var transactionHash = await ercInterfaceService.Transfer(transfer.TokenAddress,
                    addressUtil.ConvertToChecksumAddress(transfer.IssuerAddress), //Should be in SigningService
                    tokenCfg.HotwalletAddress,
                    amount);
                await consoleLogger.WriteInfoAsync(nameof(Main), transfer.ToJson(), $"Emission txHash is {transactionHash}. " +
                                                                                    $"Waiting for completion");

                WaitForTransactionCompleation(web3, transactionHash);

                await consoleLogger.WriteInfoAsync(nameof(Main), transfer.ToJson(), "Completed.");
            }

            await consoleLogger.WriteInfoAsync(nameof(Main), "", "Completed processing all tokens.");

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

        static (string abi, string bytecode) GetContractDeploymentForTokenType(TokenType type)
        {
            string abiPath = null;
            string byteCodePath = null;
            string abi = null;
            string bytecode = null;

            switch (type)
            {
                case TokenType.Emissive:
                    abiPath = "EmissiveErc223Token.abi";
                    byteCodePath = "EmissiveErc223Token.bin";
                    break;
                case TokenType.NonEmissive:
                    abiPath = "NonEmissiveErc223Token.abi";
                    byteCodePath = "NonEmissiveErc223Token.bin";
                    break;
                case TokenType.LuCyToken:
                    abiPath = "LyCI.abi";
                    byteCodePath = "LyCI.bin";
                    break;
                default:
                    throw new NotImplementedException();
            }

            abiPath = Path.Combine("Contracts", abiPath);
            byteCodePath = Path.Combine("Contracts", byteCodePath);
            abi = File.ReadAllText(abiPath);
            bytecode = File.ReadAllText(byteCodePath);

            return (abi, bytecode);
        }

        public static void WaitForTransactionCompleation(IWeb3 web3, string transactioHash)
        {
            // get contract transaction
            TransactionReceipt receipt;
            while ((receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactioHash).Result) == null)
            {
                Task.Delay(350).Wait();
            }
        }
    }
}

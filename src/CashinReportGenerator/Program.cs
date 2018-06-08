using AzureRepositories;
using AzureStorage.Tables;
using BusinessModels;
using Common;
using Common.Log;
using Core.Settings;
using EthereumSamuraiApiCaller;
using EthereumSamuraiApiCaller.Models;
using Lykke.Service.EthereumCore.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Web3;
using RabbitMQ;
using Services;
using Services.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Lykke.Job.EthereumCore.Config;
using Lykke.Service.EthereumCore.AzureRepositories;
using Lykke.Service.EthereumCore.BusinessModels;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.EthereumCore.Services.Coins;
using Lykke.Service.EthereumCore.Services.Utils;
using Lykke.Service.RabbitMQ;
using Lykke.SettingsReader;

namespace CashinReportGenerator
{
    class Program
    {
        public class UserBalanceModel
        {
            public string ClientId { get; set; }

            public string WalletBalance { get; set; }

            public string BlockchainAmount { get; set; }

            public DateTime DateTimeUtc { get; set; }
        }

        public class TransactionFeeModel
        {
            public string SenderAddress { get; set; }

            public string RecieverAddress { get; set; }

            public string EthAmount { get; set; }

            public DateTime DateTimeUtc { get; set; }

            public string TransactionHash { get; set; }

            public string FeeInEth { get; set; } //Trade, PrivateWallet
        }

        public class ExternalTransactionModel
        {
            public string ClientId { get; set; }

            public string SenderAddress { get; set; }

            public string RecieverAddress { get; set; }

            public string EthAmount { get; set; }

            public DateTime DateTimeUtc { get; set; }

            public string TransactionHash { get; set; }

            public string RecieverType { get; set; } //Trade, PrivateWallet
        }


        static void Main(string[] args)
        {
            IServiceProvider ServiceProvider;
            var configurationBuilder = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json").AddEnvironmentVariables();
            var configuration = configurationBuilder.Build();

            var settings = GetCurrentSettingsFromUrl();

            IServiceCollection collection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            collection.AddSingleton(settings);
            collection.AddSingleton<IBaseSettings>(settings.CurrentValue.EthereumCore);
            collection.AddSingleton<ISlackNotificationSettings>(settings.CurrentValue.SlackNotifications);
            collection.AddSingleton(settings.Nested(x => x.EthereumCore));
            collection.AddSingleton(settings.CurrentValue);
            var consoleLogger = new LogToConsole();
            collection.AddSingleton<ILog>(consoleLogger);

            RegisterReposExt.RegisterAzureQueues(collection, settings.Nested(x => x.EthereumCore), settings.Nested(x => x.SlackNotifications));
            RegisterReposExt.RegisterAzureStorages(collection, settings.Nested(x => x.EthereumCore), settings.Nested(x => x.SlackNotifications));
            ServiceProvider = collection.BuildServiceProvider();
            RegisterRabbitQueueEx.RegisterRabbitQueue(collection, settings.Nested(x => x.EthereumCore), ServiceProvider.GetService<ILog>());
            Lykke.Service.EthereumCore.Services.RegisterDependency.RegisterServices(collection);
            Lykke.Job.EthereumCore.Config.RegisterDependency.RegisterJobs(collection);
            //var web3 = ServiceProvider.GetService<Web3>();
            //web3.Eth.GetBalance.SendRequestAsync("");
            // web3.Eth.Transactions.SendTransaction.SendRequestAsync(new Nethereum.RPC.Eth.DTOs.TransactionInput()
            //{
            //    
            //}).Result;
            //var key = EthECKey.GenerateKey().GetPrivateKeyAsBytes();
            //var stringKey = Encoding.Unicode.GetString(key);
            ServiceProvider = collection.BuildServiceProvider();
            ServiceProvider.ActivateRequestInterceptor();

            //string coinAdapterAddress = settings.CoinAdapterAddress;
            //string balancesInfoConnString = settings.BalancesInfoConnString;
            //string bitCoinQueueConnectionString = settings.BitCoinQueueConnectionString;
            //string clientPersonalInfoConnString = settings.ClientPersonalInfoConnString;
            //string ethAssetId = settings.EthAssetId;

            //var log = ServiceProvider.GetService<ILog>();
            //var bcnRepositoryReader = new BcnClientCredentialsRepository(
            //        new AzureTableStorage<BcnCredentialsRecordEntity>(clientPersonalInfoConnString,
            //            "BcnClientCredentials", log));
            //var pendingActions = new EthererumPendingActionsRepository(
            //        new AzureTableStorage<EthererumPendingActionEntity>(bitCoinQueueConnectionString,
            //            "EthererumPendingActions", log));
            //var privateWalletsReader = new PrivateWalletsRepository(
            //        new AzureTableStorage<PrivateWalletEntity>(clientPersonalInfoConnString,
            //            "PrivateWallets", log));
            //var wallets = new WalletsRepository(new AzureTableStorage<WalletEntity>(balancesInfoConnString, "Accounts", log));
            var assetContractService = ServiceProvider.GetService<AssetContractService>();
            var samuraiApi = ServiceProvider.GetService<IEthereumSamuraiApi>();
            var ethPrecision = BigInteger.Pow(10, 18);
            string command = "-1";
            Console.WriteLine("Type 0 - to make feesReport for address");
            Console.WriteLine("Type 1 - to make cashinReport");
            Console.WriteLine("Type 2 - to make balance report");
            Console.WriteLine("Type 3 - to fill pending actions for users");

            Console.WriteLine("Type exit - to quit");

            while (command != "exit")
            {
                command = Console.ReadLine();

                switch (command)
                {
                    case "0":
                    {
                        string address = GetUserInputWithWalidation<string>("Type address to generate  fee report",
                            "WrongAddress", (ad) =>
                            {
                                var validationService =
                                    (IExchangeContractService) ServiceProvider.GetService(
                                        typeof(IExchangeContractService));
                                var isValid = validationService.IsValidAddress(ad);

                                return (isValid, isValid ? ad : "Address is not a valid deposit address");
                            });

                        using (var streamWriter = new StreamWriter("feeReport"))
                        using (var csvWriter = new CsvHelper.CsvWriter(streamWriter, false))
                        {
                            Console.WriteLine("Fee report generation started!");

                            int count = 1000;
                            int start = 0;
                            Console.WriteLine($"Asking Samurai about {address}");
                            IEnumerable<AddressHistoryModel> batchRead = null;

                            do
                            {
                                RetryPolicy.ExecuteAsync(async () =>
                                {
                                    batchRead = await GetAddressHistory(samuraiApi, new AddressTransaction()
                                    {
                                        Address = wallet.WalletAddress,
                                        Count = count,
                                        Start = start,
                                    });
                                }, 5, 100).Wait();

                                foreach (var item in batchRead)
                                {
                                    if (item.From.ToLower() == address.ToLower())
                                    {
                                        continue;
                                    }

                                    //var amount = BigInteger.Parse(item.Value);
                                    var itemFee = (BigInteger.Parse(item.GasPrice) * BigInteger.Parse(item.GasUsed)).ToString();
                                    TransactionFeeModel model = new TransactionFeeModel()
                                    {
                                        DateTimeUtc = item.BlockTimeUtc,
                                        EthAmount = ConvertFromContract(item.Value, 18, 18).ToString(),
                                        RecieverAddress = item.To,
                                        SenderAddress = item.From,
                                        TransactionHash = item.TransactionHash,
                                        FeeInEth = ConvertFromContract(itemFee, 18, 18).ToString(),
                                    };

                                    csvWriter.WriteRecord<ExternalTransactionModel>(model);
                                    csvWriter.NextRecord();

                                    Console.WriteLine($"Written ${model.ToJson()}");
                                }

                                Console.WriteLine($"Requested {address} - {start} - {count}");

                                start += count;
                            } while (batchRead != null && batchRead.Count() != 0);

                            Console.WriteLine("Fee report generation completed!");
                        }
                    }
                        break;
                    //case "3":
                            //    bcnRepositoryReader.ProcessAllAsync(async (wallet) =>
                            //    {
                            //        if (wallet.AssetId == ethAssetId)
                            //        {
                            //            BigInteger balanceOnAdapter = 0;
                            //            await RetryPolicy.ExecuteAsync(async () =>
                            //            {
                            //                balanceOnAdapter = await assetContractService.GetBalance(coinAdapterAddress, wallet.Address);
                            //                if (balanceOnAdapter > 0)
                            //                {
                            //                    await pendingActions.CreateAsync(wallet.ClientId, Guid.NewGuid().ToString());
                            //                    Console.WriteLine($"ClientId- {wallet.ClientId} added");
                            //                }
                            //            }, 3, 100);
                            //        }

                            //        Console.WriteLine($"ClientId- {wallet.ClientId} processed");
                            //    }).Wait();
                            //    break;
                            //case "1":
                            //    MakeCsvCashinReport(ethAssetId, bcnRepositoryReader, privateWalletsReader, samuraiApi);
                            //    break;
                            //case "2":
                            //    using (var streamWriter = new StreamWriter("BalancesReport"))
                            //    using (var csvWriter = new CsvHelper.CsvWriter(streamWriter, false))
                            //    {
                            //        try
                            //        {

                            //            csvWriter.WriteHeader<UserBalanceModel>();
                            //            csvWriter.NextRecord();

                            //            bcnRepositoryReader.ProcessAllAsync(async (wallet) =>
                            //        {
                            //            if (wallet.AssetId == ethAssetId)
                            //            {
                            //                double walletBalance = 0;
                            //                BigInteger balanceOnAdapter = 0;
                            //                double balanceOnAdapterCalculated = 0;
                            //                await RetryPolicy.ExecuteAsync(async () =>
                            //                {
                            //                    walletBalance = await wallets.GetWalletBalanceAsync(wallet.ClientId, ethAssetId);
                            //                    balanceOnAdapter = await assetContractService.GetBalance(coinAdapterAddress, wallet.Address);
                            //                }, 3, 100);

                            //                {
                            //                    string balanceOnAdapterString = balanceOnAdapter.ToString();
                            //                    balanceOnAdapterCalculated = (double)ConvertFromContract(balanceOnAdapterString, 18, 6);
                            //                }

                            //                if (walletBalance != balanceOnAdapterCalculated)
                            //                {
                            //                    UserBalanceModel model = new UserBalanceModel()
                            //                    {
                            //                        ClientId = wallet.ClientId,
                            //                        BlockchainAmount = balanceOnAdapterCalculated.ToString(),
                            //                        DateTimeUtc = DateTime.UtcNow,
                            //                        WalletBalance = walletBalance.ToString()
                            //                    };

                            //                    csvWriter.WriteRecord<UserBalanceModel>(model);
                            //                    csvWriter.NextRecord();
                            //                }

                            //                Console.WriteLine($"Requested {wallet.ClientId} {wallet.Address} {walletBalance} {balanceOnAdapterCalculated}");
                            //            }
                            //            else
                            //            {
                            //                Console.WriteLine($"Skipping {wallet.ClientId} {wallet.Address}");
                            //            }
                            //        }).Wait();
                            //        }
                            //        catch (Exception e)
                            //        {
                            //            Console.WriteLine($"Completly broken {e.Message} - {e.StackTrace}");
                            //        }
                            //    }

                            //    Console.WriteLine("Completed for bcn repo");
                            //    break;
                            default:
                        break;
                        }
                }
            }

            private static void MakeCsvCashinReport(string ethAssetId,
                BcnClientCredentialsRepository bcnRepositoryReader,
                PrivateWalletsRepository privateWalletsReader,
                IEthereumSamuraiApi samuraiApi)
            {
                try
                {
                    using (var streamWriter = new StreamWriter("csvReport"))
                    using (var csvWriter = new CsvHelper.CsvWriter(streamWriter, false))
                    {
                        csvWriter.WriteHeader<ExternalTransactionModel>();
                        csvWriter.NextRecord();

                        #region PrivateWallets

                        try
                        {
                            privateWalletsReader.ProcessAllAsync(async (wallet) =>
                            {
                                if (wallet.BlockchainType == Lykke.Service.Assets.Client.Models.Blockchain.Ethereum)
                                {
                                    await RetryPolicy.ExecuteAsync(async () =>
                                    {
                                        String address = wallet.WalletAddress;

                                        int count = 1000;
                                        int start = 0;
                                        Console.WriteLine($"Asking Samurai about {address}");
                                        IEnumerable<AddressHistoryModel> batchRead = null;

                                        do
                                        {
                                            batchRead = await GetAddressHistory(samuraiApi, new AddressTransaction()
                                            {
                                                Address = wallet.WalletAddress,
                                                Count = count,
                                                Start = start,
                                            });

                                            foreach (var item in batchRead)
                                            {
                                                if (item.From.ToLower() == address.ToLower())
                                                {
                                                    continue;
                                                }

                                            //var amount = BigInteger.Parse(item.Value);
                                            ExternalTransactionModel model = new ExternalTransactionModel()
                                                {
                                                    ClientId = wallet.ClientId,
                                                    DateTimeUtc = item.BlockTimeUtc,
                                                    EthAmount = ConvertFromContract(item.Value, 18, 18).ToString(),
                                                    RecieverAddress = item.To,
                                                    SenderAddress = item.From,
                                                    RecieverType = "PrivateWallet",
                                                    TransactionHash = item.TransactionHash
                                                };

                                                csvWriter.WriteRecord<ExternalTransactionModel>(model);
                                                csvWriter.NextRecord();

                                                Console.WriteLine($"Written ${model.ToJson()}");
                                            }

                                            Console.WriteLine($"Requested {address} - {start} - {count}");

                                            start += count;
                                        }
                                        while (batchRead != null && batchRead.Count() != 0);

                                    }, 3, 100);
                                }
                                else
                                {
                                    Console.WriteLine($"Skipping {wallet.WalletAddress}");
                                }
                            }).Wait();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Completly broken {e.Message} - {e.StackTrace}");
                        }

                        Console.WriteLine("Completed for privateWallets");

                        #endregion

                        #region bcn

                        Console.WriteLine("Started for TradeWWallet");

                        try
                        {
                            bcnRepositoryReader.ProcessAllAsync(async (wallet) =>
                            {
                                if (wallet.AssetId == ethAssetId)
                                {
                                    await RetryPolicy.ExecuteAsync(async () =>
                                    {
                                        String address = wallet.AssetAddress;

                                        int count = 1000;
                                        int start = 0;
                                        Console.WriteLine($"Asking Samurai about {address}");
                                        IEnumerable<AddressHistoryModel> batchRead = null;

                                        do
                                        {
                                            batchRead = await GetAddressHistory(samuraiApi, new AddressTransaction()
                                            {
                                                Address = address,
                                                Count = count,
                                                Start = start,
                                            });

                                            foreach (var item in batchRead)
                                            {
                                                if (item.From.ToLower() == address.ToLower()
                                                || item.Value == "0")
                                                {
                                                    continue;
                                                }

                                            //var amount = BigInteger.Parse(item.Value);
                                            ExternalTransactionModel model = new ExternalTransactionModel()
                                                {
                                                    ClientId = wallet.ClientId,
                                                    DateTimeUtc = item.BlockTimeUtc,
                                                    EthAmount = ConvertFromContract(item.Value, 18, 18).ToString(),
                                                    RecieverAddress = item.To,
                                                    SenderAddress = item.From,
                                                    RecieverType = "TradeWallet",
                                                    TransactionHash = item.TransactionHash
                                                };

                                                csvWriter.WriteRecord<ExternalTransactionModel>(model);
                                                csvWriter.NextRecord();

                                                Console.WriteLine($"Written ${model.ToJson()}");
                                            }

                                            Console.WriteLine($"Requested {address} - {start} - {count}");

                                            start += count;
                                        }
                                        while (batchRead != null && batchRead.Count() != 0);

                                    }, 3, 100);
                                }
                                else
                                {
                                    Console.WriteLine($"Skipping {wallet.AssetAddress}");
                                }
                            }).Wait();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Completly broken {e.Message} - {e.StackTrace}");
                        }

                        Console.WriteLine("Completed for bcn repo");

                        #endregion

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error {e.Message} - {e.StackTrace}");
                }
            }

            static IReloadingManager<AppSettings> GetCurrentSettingsFromUrl()
            {
                FileInfo fi = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
                var location = Path.Combine(fi.DirectoryName, "..", "..", "..");
                var builder = new ConfigurationBuilder()
                    .SetBasePath(location)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();
                var configuration = builder.Build();
                var connString = configuration.GetConnectionString("ConnectionString");
                var settings = configuration.LoadSettings<AppSettings>();

                return settings;
            }

            public static async Task<IEnumerable<AddressHistoryModel>> GetAddressHistory(IEthereumSamuraiApi ethereumSamuraiApi,
                AddressTransaction addressTransactions)
            {
                var historyResponseRaw = await ethereumSamuraiApi.ApiAddressHistoryByAddressGetAsync(
                    addressTransactions.Address, null, null, addressTransactions.Start, addressTransactions.Count);
                var addressHistoryResponse = historyResponseRaw as FilteredAddressHistoryResponse;
                ThrowOnError(historyResponseRaw);
                int responseCount = addressHistoryResponse.History?.Count ?? 0;
                List<AddressHistoryModel> result = new List<AddressHistoryModel>(responseCount);

                foreach (var item in addressHistoryResponse.History)
                {
                    result.Add(
                        new AddressHistoryModel()
                        {
                            MessageIndex = item.MessageIndex.Value,
                            TransactionIndexInBlock = item.TransactionIndex.Value,
                            BlockNumber = (ulong)item.BlockNumber.Value,
                            BlockTimestamp = (uint)item.BlockTimestamp.Value,
                            BlockTimeUtc = DateUtils.UnixTimeStampToDateTimeUtc(item.BlockTimestamp.Value),
                            From = item.FromProperty,
                            HasError = item.HasError.Value,
                            To = item.To,
                            TransactionHash = item.TransactionHash,
                            Value = item.Value,
                            GasPrice = item.GasPrice,
                            GasUsed = item.GasUsed
                        });
                }

                return result;
            }

            private static void ThrowOnError(object transactionResponse)
            {
                if (transactionResponse == null)
                {
                    var exception = transactionResponse as ApiException;
                    var errorMessage = exception?.Error?.Message ?? "Response is empty";

                    throw new Exception(errorMessage);
                }
            }

            public static decimal ConvertFromContract(string amount, int multiplier, int accuracy)
            {
                if (accuracy > multiplier)
                    throw new ArgumentException("accuracy > multiplier");

                multiplier -= accuracy;

                var val = BigInteger.Parse(amount);
                var res = (decimal)(val / BigInteger.Pow(10, multiplier));
                res /= (decimal)Math.Pow(10, accuracy);

                return res;
            }

            private static T GetUserInputWithWalidation<T>(string typeOfInput,
                string messageOnWrongInput,
                Func<string, (bool IsValid, T Result)> validFunc)
            {
                string input = "";

                do
                {
                    Console.Write($"{typeOfInput}: ");
                    input = Console.ReadLine();
                    var validationResult = validFunc(input);

                    if (validationResult.IsValid)
                    {
                        return validationResult.Result;
                    }

                    Console.WriteLine($"Try Again! Error: {validationResult.Result.ToString()}");

                } while (true);
            }


        }

        public static class Extensions
        {
            public static TConvert ConvertTo<TConvert>(this object entity) where TConvert : new()
            {
                var convertProperties = TypeDescriptor.GetProperties(typeof(TConvert)).Cast<PropertyDescriptor>();
                var entityProperties = TypeDescriptor.GetProperties(entity).Cast<PropertyDescriptor>();

                var convert = new TConvert();

                foreach (var entityProperty in entityProperties)
                {
                    var property = entityProperty;
                    var convertProperty = convertProperties.FirstOrDefault(prop => prop.Name == property.Name);
                    if (convertProperty != null)
                    {
                        convertProperty.SetValue(convert, Convert.ChangeType(entityProperty.GetValue(entity), convertProperty.PropertyType));
                    }
                }

                return convert;
            }
        }

        public class SettingsWrapperExtended
        {
            public string ClientPersonalInfoConnString { get; set; }
            public string EthAssetId { get; set; }
            public string BalancesInfoConnString { get; set; }
            public string CoinAdapterAddress { get; set; }
            public string BitCoinQueueConnectionString { get; set; }
        }
    }

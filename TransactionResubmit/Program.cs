using AzureRepositories;
using Common.Log;
using Core;
using Core.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ;
using Services;
using System;
using System.IO;
using System.Numerics;

namespace TransactionResubmit
{
    class Program
    {
        static IServiceProvider ServiceProvider;
        static void Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json").AddEnvironmentVariables();
            var configuration = configurationBuilder.Build();

            var settings = GetCurrentSettings();

            IServiceCollection collection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            collection.AddSingleton<IBaseSettings>(settings.EthereumCore);
            collection.AddSingleton<ISlackNotificationSettings>(settings.SlackNotifications);

            RegisterReposExt.RegisterAzureLogs(collection, settings.EthereumCore, "");
            RegisterReposExt.RegisterAzureQueues(collection, settings.EthereumCore, settings.SlackNotifications);
            RegisterReposExt.RegisterAzureStorages(collection, settings.EthereumCore, settings.SlackNotifications);
            ServiceProvider = collection.BuildServiceProvider();
            RegisterRabbitQueueEx.RegisterRabbitQueue(collection, settings.EthereumCore, ServiceProvider.GetService<ILog>());
            RegisterDependency.RegisterServices(collection);
            ServiceProvider = collection.BuildServiceProvider();
            //File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "TransactionsForResubmit.json"),
            //    Newtonsoft.Json.JsonConvert.SerializeObject(new ResubmitModel()
            //    {
            //        Transactions = new System.Collections.Generic.List<ResubmitTransactionModel>()
            //        {
            //            new ResubmitTransactionModel()
            //            {
            //                Amount = "",
            //                Change = "",
            //                CoinAdapterAddress = "",
            //                FromAddress = "",
            //                Id = Guid.NewGuid(),
            //                OperationType ="",
            //                SignFrom ="",
            //                SignTo ="",
            //                ToAddress ="",
            //            }
            //        }
            //    }));

            Console.WriteLine($"Type 0 to exit");
            Console.WriteLine($"Type 1 to resubmit transaction");
            var command = "";

            do
            {
                command = Console.ReadLine();
                switch (command)
                {
                    case "1":
                        TransactionResubmitTransaction();
                        break;
                    default:
                        break;
                }
            }
            while (command != "0");

            Console.WriteLine("Exited");
        }

        private static void TransactionResubmitTransaction()
        {
            try
            {
                var pendingOperationService = ServiceProvider.GetService<IPendingOperationService>();
                ResubmitModel resubmitModel;
                Console.WriteLine("ResubmittingStarted");
                using (var streamRead = File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), "TransactionsForResubmit.json")))
                using (var stream = new StreamReader(streamRead))
                {
                    string content = stream.ReadToEnd();
                    resubmitModel = Newtonsoft.Json.JsonConvert.DeserializeObject<ResubmitModel>(content);
                }

                foreach (var item in resubmitModel.Transactions)
                {
                    var amount = (BigInteger.Parse(item.Amount) + 1);
                    Console.WriteLine($"Amount updated to {amount.ToString()}");
                    Console.WriteLine($"Performing {item.OperationType}");
                    switch (item.OperationType)
                    {
                        case OperationTypes.Cashout:
                            pendingOperationService.CashOut(item.Id, item.CoinAdapterAddress, item.FromAddress, item.ToAddress, amount, item.SignFrom ?? "").Wait();
                            break;
                        case OperationTypes.Transfer:
                            pendingOperationService.Transfer(item.Id, item.CoinAdapterAddress, item.FromAddress, item.ToAddress, amount, item.SignFrom ?? "").Wait();
                            break;
                        case OperationTypes.TransferWithChange:
                            var change = BigInteger.Parse(item.Change);
                            pendingOperationService.TransferWithChange(item.Id, item.CoinAdapterAddress, item.FromAddress, item.ToAddress, amount, item.SignFrom ?? "", change, item.SignTo ?? "").Wait();
                            break;
                        default:
                            throw new Exception("Specify operation Type");

                    }
                    Console.WriteLine("Operation resubmitted");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}, {e.StackTrace}");
            }
            Console.WriteLine();
        }

        static SettingsWrapper GetCurrentSettings()
        {
            FileInfo fi = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
            var location = Path.Combine(fi.DirectoryName, "..", "..", "..");
            var builder = new ConfigurationBuilder()
                .SetBasePath(location)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            var configuration = builder.Build();
            var settings = GeneralSettingsReader.ReadGeneralSettings<SettingsWrapper>(configuration.GetConnectionString("ConnectionString"));

            return settings;
        }
    }
}
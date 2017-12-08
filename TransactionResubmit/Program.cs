using AzureRepositories;
using Common.Log;
using Core;
using Core.Repositories;
using Core.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json;
using RabbitMQ;
using Services;
using Services.Coins;
using Services.Coins.Models;
using Services.New.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

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

            var web3 = ServiceProvider.GetService<Web3>();

            try
            {
                var blockNumber = web3.Eth.Blocks.GetBlockNumber.SendRequestAsync().Result;
                Console.WriteLine($"RPC Works! {blockNumber.Value.ToString()}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Rpc does not work at all! {e.Message}");
            }
                Console.WriteLine($"Type 0 to exit");
            Console.WriteLine($"Type 1 to SHOW pending Transactions");
            Console.WriteLine($"Type 2 to REPEAT all operation without hash");
            Console.WriteLine($"Type 3 to CHECK pending operations");
            Console.WriteLine($"Type 4 to SCAN transfer contracts for issues");
            Console.WriteLine($"Type 5 to REASSIGN contracts");
            Console.WriteLine($"Type 6 to List all sucesful coin events");
            Console.WriteLine($"Type 7 to RESUBMIT all succesful coin events(except cashin)");
            Console.WriteLine($"Type 8 to RESUBMIT all cashin coin events");
            Console.WriteLine($"Type 9 to REMOVE DUPLICATE user transfer wallet locks");
            Console.WriteLine($"Type 10 to move from pending-poison to processing");
            Console.WriteLine($"Type 11 to PUT EVERYTHING IN PENDING WITH zero dequeue count");
            var command = "";

            do
            {
                command = Console.ReadLine();
                switch (command)
                {
                    case "1":
                        ShowPendingTransactionsAmount();
                        break;
                    case "2":
                        OperationResubmit();
                        break;
                    case "3":
                        OperationCheck();
                        break;
                    case "4":
                        GetAllFailedAssignments();
                        break;
                    case "5":
                        StartReassignment();
                        break;
                    case "6":
                        ListUnPublishedCoinEvents();
                        break;
                    case "7":
                        ResubmittUnPublishedCoinEventsWithMatches();
                        break;
                    case "8":
                        ResubmittUnPublishedCoinEventsCashinOnly();
                        break;
                    case "9":
                        RemoveDuplicateUserTransferWallets();
                        break;
                    case "10":
                        MoveFromPoisonToProcessing();
                        break;
                    case "11":
                        MoveFromPendingAndPoisonToProcessing();
                        break;
                    default:
                        break;
                }
            }
            while (command != "0");

            Console.WriteLine("Exited");
        }

        private static void MoveFromPendingAndPoisonToProcessing()
        {
            try
            {
                Console.WriteLine("Are you sure?: Y/N");
                var input = Console.ReadLine();
                if (input.ToLower() != "y")
                {
                    Console.WriteLine("Cancel");
                    return;
                }
                Console.WriteLine("Started");

                MoveToPendingOperationQueue(Constants.PendingOperationsQueue);

                Console.WriteLine("Pendin processed");
                Console.WriteLine("Waiting for poison");

                MoveToPendingOperationQueue(Constants.PendingOperationsQueue + "-poison");

                Console.WriteLine("All Processed");
            }
            catch (Exception e)
            {

            }
        }

        private static void MoveToPendingOperationQueue(string fromQueue)
        {
            var queueFactory = ServiceProvider.GetService<IQueueFactory>();
            var queuePoison = queueFactory.Build(fromQueue);
            var queue = queueFactory.Build(Constants.PendingOperationsQueue);
            var count = queuePoison.Count().Result;
            for (int i = 0; i < count; i++)
            {
                var message = queuePoison.GetRawMessageAsync().Result;

                OperationHashMatchMessage newMessage = JsonConvert.DeserializeObject<OperationHashMatchMessage>(message.AsString);
                newMessage.DequeueCount = 0;
                queue.PutRawMessageAsync(message.AsString).Wait();
                queuePoison.FinishRawMessageAsync(message).Wait();
            }
        }

        private static void MoveFromPoisonToProcessing()
        {
            try
            {
                Console.WriteLine("Are you sure?: Y/N");
                var input = Console.ReadLine();
                if (input.ToLower() != "y")
                {
                    Console.WriteLine("Cancel");
                    return;
                }
                Console.WriteLine("Started");

                var queueFactory = ServiceProvider.GetService<IQueueFactory>();
                var queuePoison = queueFactory.Build(Constants.PendingOperationsQueue + "-poison");
                var queue = queueFactory.Build(Constants.PendingOperationsQueue);
                var count = queuePoison.Count().Result;
                for (int i = 0; i < count; i++)
                {
                    var message = queuePoison.GetRawMessageAsync().Result;

                    OperationHashMatchMessage newMessage = JsonConvert.DeserializeObject<OperationHashMatchMessage>(message.AsString);

                    queue.PutRawMessageAsync(message.AsString).Wait();
                    queuePoison.FinishRawMessageAsync(message);
                }

                Console.WriteLine("All Processed");
            }
            catch (Exception e)
            {

            }
        }

        private static void ShowPendingTransactionsAmount()
        {
            var web3 = ServiceProvider.GetService<Web3>();
            var block = web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreatePending()).Result;
            var transactionsCount = block.Transactions.Count();

            Console.WriteLine($"Transactions Count = {transactionsCount}");
        }

        private static void RemoveDuplicateUserTransferWallets()
        {
            try
            {
                Console.WriteLine("Are you sure?: Y/N");
                var input = Console.ReadLine();
                if (input.ToLower() != "y")
                {
                    Console.WriteLine("Cancel");
                    return;
                }
                Console.WriteLine("Started");

                var userTransferWalletRepository = ServiceProvider.GetService<IUserTransferWalletRepository>();
                var userTransferWallets = userTransferWalletRepository.GetAllAsync().Result;

                foreach (var wallet in userTransferWallets)
                {
                    if (wallet.UserAddress != wallet.UserAddress.ToLower())
                    {
                        Console.WriteLine($"Deleting {wallet.UserAddress}");
                        userTransferWalletRepository.DeleteAsync(wallet.UserAddress, wallet.TransferContractAddress).Wait();
                    }
                }

                Console.WriteLine("All Processed");
            }
            catch (Exception e)
            {

            }
        }

        private static void ResubmittUnPublishedCoinEventsCashinOnly()
        {
            try
            {
                Console.WriteLine("Are you sure?: Y/N");
                var input = Console.ReadLine();
                if (input.ToLower() != "y")
                {
                    Console.WriteLine("Cancel");
                    return;
                }
                Console.WriteLine("Started");

                var queueFactory = ServiceProvider.GetService<IQueueFactory>();
                var queue = queueFactory.Build(Constants.TransactionMonitoringQueue);
                var coinEventRepo = ServiceProvider.GetService<ICoinEventRepository>();
                var trService = ServiceProvider.GetService<IEthereumTransactionService>();
                var events = coinEventRepo.GetAll().Result.Where(x => !string.IsNullOrEmpty(x.OperationId) && x.CoinEventType == CoinEventType.CashinStarted).ToList();
                if (events != null)
                {
                    foreach (var @event in events)
                    {
                        if (@event != null && trService.IsTransactionExecuted(@event.TransactionHash, Constants.GasForCoinTransaction).Result)
                        {
                            Console.WriteLine($"Unpublished transaction {@event.TransactionHash}");
                            queue.PutRawMessageAsync(Newtonsoft.Json.JsonConvert.SerializeObject(new CoinTransactionMessage()
                            {
                                TransactionHash = @event.TransactionHash,
                                OperationId = "",
                                LastError = "FROM_CONSOLE_CASHIN",
                                PutDateTime = DateTime.UtcNow })).Wait();
                        }
                    }
                }

                Console.WriteLine("All Processed");
            }
            catch (Exception e)
            {

            }
        }

        private static void ResubmittUnPublishedCoinEventsWithMatches()
        {
            try
            {
                Console.WriteLine("Are you sure?: Y/N");
                var input = Console.ReadLine();
                if (input.ToLower() != "y")
                {
                    Console.WriteLine("Cancel");
                    return;
                }
                Console.WriteLine("Started");

                var queueFactory = ServiceProvider.GetService<IQueueFactory>();
                var queue = queueFactory.Build(Constants.TransactionMonitoringQueue);
                var coinEventRepo = ServiceProvider.GetService<ICoinEventRepository>();
                var opRepo = ServiceProvider.GetService<IOperationToHashMatchRepository>();
                var trService = ServiceProvider.GetService<IEthereumTransactionService>();
                var events = coinEventRepo.GetAll().Result.Where(x => !string.IsNullOrEmpty(x.OperationId));
                var allCoinEvents = events.ToLookup(x => x.OperationId);
                opRepo.ProcessAllAsync((matches) =>
                {
                    foreach (var match in matches)
                    {
                        if (string.IsNullOrEmpty(match.TransactionHash) || !trService.IsTransactionExecuted(match.TransactionHash, Constants.GasForCoinTransaction).Result)
                        {
                            var coinEvents = allCoinEvents[match.OperationId]?.OrderByDescending(x => x.EventTime);
                            if (coinEvents != null)
                            {
                                foreach (var @event in coinEvents)
                                {
                                    if (@event != null && trService.IsTransactionExecuted(@event.TransactionHash, Constants.GasForCoinTransaction).Result)
                                    {
                                        Console.WriteLine($"Unpublished transaction {@event.TransactionHash}");
                                        queue.PutRawMessageAsync(Newtonsoft.Json.JsonConvert.SerializeObject(new CoinTransactionMessage() { TransactionHash = @event.TransactionHash, OperationId = match.OperationId, LastError = "FROM_CONSOLE", PutDateTime = DateTime.UtcNow })).Wait();
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    return Task.FromResult(0);
                }).Wait();

                Console.WriteLine("All Processed");
            }
            catch (Exception e)
            {

            }
        }

        private static void ListUnPublishedCoinEvents()
        {
            try
            {
                List<string> allUnpublishedEvents = new List<string>();
                var coinEventRepo = ServiceProvider.GetService<ICoinEventRepository>();
                var opRepo = ServiceProvider.GetService<IOperationToHashMatchRepository>();
                var trService = ServiceProvider.GetService<IEthereumTransactionService>();
                var events = coinEventRepo.GetAll().Result.Where(x => !string.IsNullOrEmpty(x.OperationId));
                var allCoinEvents = events.ToLookup(x => x.OperationId);
                opRepo.ProcessAllAsync((matches) =>
                {
                    foreach (var match in matches)
                    {
                        if (string.IsNullOrEmpty(match.TransactionHash) || !trService.IsTransactionExecuted(match.TransactionHash, Constants.GasForCoinTransaction).Result)
                        {
                            var coinEvents = allCoinEvents[match.OperationId]?.OrderByDescending(x => x.EventTime);
                            if (coinEvents != null)
                            {
                                foreach (var @event in coinEvents)
                                {
                                    if (@event != null && trService.IsTransactionExecuted(@event.TransactionHash, Constants.GasForCoinTransaction).Result)
                                    {
                                        Console.WriteLine($"Unpublished transaction {@event.TransactionHash}");
                                        allUnpublishedEvents.Add(@event.TransactionHash);
                                        break;
                                    }
                                }
                            }
                            
                        }
                    }

                    return Task.FromResult(0);
                }).Wait();

                Console.WriteLine("All has been processed");
                File.WriteAllText("listUnPublishedCoinEvents.txt", Newtonsoft.Json.JsonConvert.SerializeObject(allUnpublishedEvents));
            }
            catch (Exception e)
            {

            }
        }

        private static void StartReassignment()
        {
            try
            {
                Console.WriteLine("Are you sure?: Y/N");
                var input = Console.ReadLine();
                if (input.ToLower() != "y")
                {
                    Console.WriteLine("Cancel Reassignment");
                    return;
                }
                Console.WriteLine("Reassignment started");

                List<string> allTransferContracts = new List<string>();
                var trService = ServiceProvider.GetService<IEthereumTransactionService>();
                var transferRepo = ServiceProvider.GetService<ITransferContractRepository>();
                var assignmentService = ServiceProvider.GetService<ITransferContractUserAssignmentQueueService>();
                var transferContractService = ServiceProvider.GetService<ITransferContractService>();

                transferRepo.ProcessAllAsync((contract) =>
                {
                    var currentUser = transferContractService.GetTransferAddressUser(contract.CoinAdapterAddress, contract.ContractAddress).Result;
                    if (string.IsNullOrEmpty(currentUser) || currentUser == Constants.EmptyEthereumAddress)
                    {
                        assignmentService.PushContract(new TransferContractUserAssignment()
                        {
                            CoinAdapterAddress = contract.CoinAdapterAddress,
                            TransferContractAddress = contract.ContractAddress,
                            UserAddress = contract.UserAddress,
                            PutDateTime = DateTime.UtcNow
                        });
                        Console.WriteLine($"Reassign - {contract.ContractAddress} - -_-");
                        allTransferContracts.Add(contract.ContractAddress);
                    };

                    return Task.FromResult(0);
                }).Wait();

                File.WriteAllText("report.txt", Newtonsoft.Json.JsonConvert.SerializeObject(allTransferContracts));
                Console.WriteLine("Reassignment completed");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error during check: {e.Message} - {e.StackTrace}");
            }
        }


        private static void GetAllFailedAssignments()
        {
            try
            {
                List<string> allTransferContracts = new List<string>();
                var trService = ServiceProvider.GetService<IEthereumTransactionService>();
                var transferRepo = ServiceProvider.GetService<ITransferContractRepository>();
                var transferContractService = ServiceProvider.GetService<ITransferContractService>();
                transferRepo.ProcessAllAsync((contract) =>
                {
                    var currentUser = transferContractService.GetTransferAddressUser(contract.CoinAdapterAddress, contract.ContractAddress).Result;
                    if (string.IsNullOrEmpty(currentUser) || currentUser == Constants.EmptyEthereumAddress)
                    {
                        Console.WriteLine($"Broken - {contract.ContractAddress} - X_X");
                        allTransferContracts.Add(contract.ContractAddress);
                    }

                    return Task.FromResult(0);
                }).Wait();

                File.WriteAllText("report.txt", Newtonsoft.Json.JsonConvert.SerializeObject(allTransferContracts));

            }
            catch (Exception e)
            {
                Console.WriteLine($"Error during check: {e.Message} - {e.StackTrace}");
            }
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


        private static void HashResubmit()
        {
            var queueFactory = ServiceProvider.GetService<IQueueFactory>();
            var queue = queueFactory.Build(Constants.TransactionMonitoringQueue);
            RabbitList resubmitModel;
            using (var streamRead = File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), "HashForResubmit.json")))
            using (var stream = new StreamReader(streamRead))
            {
                string content = stream.ReadToEnd();
                resubmitModel = Newtonsoft.Json.JsonConvert.DeserializeObject<RabbitList>(content);
            }

            Console.WriteLine("ResubmittingStarted");
            resubmitModel.Transactions.ForEach(tr =>
            {
                queue.PutRawMessageAsync(Newtonsoft.Json.JsonConvert.SerializeObject(new CoinTransactionMessage() { TransactionHash = tr.TransactionHash, PutDateTime = DateTime.UtcNow })).Wait();
            });
        }

        private static void OperationCheck()
        {
            try
            {
                var list = new List<string>();
                Console.WriteLine("CheckingOperation");
                IEthereumTransactionService coinTransactionService = ServiceProvider.GetService<IEthereumTransactionService>();
                var operationToHashMatchRepository = ServiceProvider.GetService<IOperationToHashMatchRepository>();
                operationToHashMatchRepository.ProcessAllAsync((items) =>
                {
                    foreach (var item in items)
                    {
                        if (string.IsNullOrEmpty(item.TransactionHash) || !coinTransactionService.IsTransactionExecuted(item.TransactionHash, Constants.GasForCoinTransaction).Result)
                        {
                            Console.WriteLine($"Operation is dead {item.OperationId}");
                            list.Add(item.OperationId);
                        }
                    }
                    return Task.FromResult(0);
                }).Wait();

                File.WriteAllText("reportOperations.txt", Newtonsoft.Json.JsonConvert.SerializeObject(list));
                Console.WriteLine("Report completed");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}, {e.StackTrace}");
            }
            Console.WriteLine();
        }

        private static void OperationResubmit()
        {
            try
            {
                Console.WriteLine("Are you sure?: Y/N");
                var input = Console.ReadLine();
                if (input.ToLower() != "y")
                {
                    Console.WriteLine("Cancel Resubmit");
                    return;
                }

                var queueFactory = ServiceProvider.GetService<IQueueFactory>();
                IEthereumTransactionService coinTransactionService = ServiceProvider.GetService<IEthereumTransactionService>();
                var queue = queueFactory.Build(Constants.PendingOperationsQueue);
                var operationToHashMatchRepository = ServiceProvider.GetService<IOperationToHashMatchRepository>();
                operationToHashMatchRepository.ProcessAllAsync((items) =>
                {
                    foreach (var item in items)
                    {
                        if (string.IsNullOrEmpty(item.TransactionHash) || !coinTransactionService.IsTransactionExecuted(item.TransactionHash, Constants.GasForCoinTransaction).Result)
                        {
                            Console.WriteLine($"Resubmitting {item.OperationId}");
                            queue.PutRawMessageAsync(Newtonsoft.Json.JsonConvert.SerializeObject(new OperationHashMatchMessage() { OperationId = item.OperationId })).Wait();
                        }
                    }
                    return Task.FromResult(0);
                }).Wait();

                Console.WriteLine("Resubmitted");
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
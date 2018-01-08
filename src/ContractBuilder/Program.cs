using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Settings;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Services;
using Nethereum.Web3;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Text;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using AzureRepositories;
using SigningServiceApiCaller;
using SigningServiceApiCaller.Models;
using Services.Coins;
using RabbitMQ;
using Common.Log;
using Services.New;
//using Core.Repositories;
using Core;
using EthereumApi.Models;
using System.Numerics;
using Core.Repositories;
using Nethereum.Util;
using EthereumJobs.Job;
using EthereumContract = Core.Settings.EthereumContract;

namespace ContractBuilder
{
    public class DebugEvent
    {
        /* int _eventNumber,
        string _value*/
        [Parameter("int", "_eventNumber", 1, false)]
        public string EventNumber { get; set; }

        [Parameter("string", "_value", 2, false)]
        public string Value { get; set; }
    }
    public class Program
    {
        public static IServiceProvider ServiceProvider { get; set; }

        public static void Main(string[] args)
        {
            var exit = false;

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").AddEnvironmentVariables();
            var configuration = configurationBuilder.Build();

            var settings = GetCurrentSettingsFromUrl();
            SaveSettings(settings);

            IServiceCollection collection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            collection.AddSingleton(settings);
            collection.AddSingleton<IBaseSettings>(settings.EthereumCore);
            collection.AddSingleton<ISlackNotificationSettings>(settings.SlackNotifications);

            RegisterReposExt.RegisterAzureLogs(collection, settings.EthereumCore, "");
            RegisterReposExt.RegisterAzureQueues(collection, settings.EthereumCore, settings.SlackNotifications);
            RegisterReposExt.RegisterAzureStorages(collection, settings.EthereumCore, settings.SlackNotifications);
            ServiceProvider = collection.BuildServiceProvider();
            RegisterRabbitQueueEx.RegisterRabbitQueue(collection, settings.EthereumCore, ServiceProvider.GetService<ILog>());
            RegisterDependency.RegisterServices(collection);
            EthereumJobs.Config.RegisterDependency.RegisterJobs(collection);
            //var web3 = ServiceProvider.GetService<Web3>();
            //web3.Eth.GetBalance.SendRequestAsync("");
            // web3.Eth.Transactions.SendTransaction.SendRequestAsync(new Nethereum.RPC.Eth.DTOs.TransactionInput()
            //{
            //    
            //}).Result;
            //var key = EthECKey.GenerateKey().GetPrivateKeyAsBytes();
            //var stringKey = Encoding.Unicode.GetString(key);
            GetAllContractInJson();
            ServiceProvider = collection.BuildServiceProvider();
            ServiceProvider.ActivateRequestInterceptor();
            //var lykkeSigningAPI = ServiceProvider.GetService<ILykkeSigningAPI>();
            //lykkeSigningAPI.ApiEthereumAddkeyPost(new AddKeyRequest()
            //{
            //    Key = "",
            //});

            var eventService = ServiceProvider.GetService<ITransactionEventsService>();
            eventService.IndexCashinEventsForAdapter("0x1c4ca817d1c61f9c47ce2bec9d7106393ff981ce", "0x512867d36f1d6ee43f2056a7c41606133bce514fbc8e911c1834eeae80800ceb").Wait();




            #region StatusExamples
            //var service = ServiceProvider.GetService<ICoinTransactionService>();
            //{
            //    //fail
            //    var x = service.ProcessTransaction(new Services.Coins.Models.CoinTransactionMessage()
            //    {
            //        TransactionHash = "0xf86efe1b8de285b8255519ca7d0ac76088132e6c5306f88dfc27312c6d7127ea",
            //    }).Result;
            //}

            //{
            //    //ok
            //    var x = service.ProcessTransaction(new Services.Coins.Models.CoinTransactionMessage()
            //    {
            //        TransactionHash = "0xa237230df97a0d6710241597a0186662928afa373c13b8d4eac86f36aa678985",
            //    }).Result;
            //}

            //{
            //    //fail
            //    var x = service.ProcessTransaction(new Services.Coins.Models.CoinTransactionMessage()
            //    {
            //        TransactionHash = "0xb63ac4f94006cbbfe58a1d651e173c56dc74a45e4d1141ac57fc51a0d4202e95",
            //    }).Result;
            //}

            //{
            //    //fail
            //    var x = service.ProcessTransaction(new Services.Coins.Models.CoinTransactionMessage()
            //    {
            //        TransactionHash = "0x1df50ee79d0af8b433f7f0be2a84cbb5dc3e29e5822e78b9c6a7ec33d027e286",
            //    }).Result;
            //}

            //{
            //    //fail
            //    var x = service.ProcessTransaction(new Services.Coins.Models.CoinTransactionMessage()
            //    {
            //        TransactionHash = "0xa3d4c1da523273371fe45c928b9236b353976e7b9e6d2b31e659f7a4c781a764",
            //    }).Result;
            //}

            #endregion

            //0xf86efe1b8de285b8255519ca7d0ac76088132e6c5306f88dfc27312c6d7127ea      0x0 
            //0xa237230df97a0d6710241597a0186662928afa373c13b8d4eac86f36aa678985      0x1
            //0xb63ac4f94006cbbfe58a1d651e173c56dc74a45e4d1141ac57fc51a0d4202e95

            var service = ServiceProvider.GetService<IErcInterfaceService>();
            service.Transfer("0x5adbf411faf2595698d80b7f93d570dd16d7f4b2", settings.EthereumCore.EthereumMainAccount,
                "0xae4d8b0c887508750ddb6b32752a82431941e2e7", System.Numerics.BigInteger.Parse("10000000000000000000")).Wait();
            //var paymentService = ServiceProvider.GetService<IPaymentService>();
            //    string result = paymentService.SendEthereum(settings.EthereumMainAccount, 
            //    "0xbb0a9c08030898cdaf1f28633f0d3c8556155482", new System.Numerics.BigInteger(5000000000000000)).Result;
            //var coinEv = ServiceProvider.GetService<ICoinEventService>();
            //var ev1 = coinEv.GetCoinEvent("0xbfb8d6a561c1a088c347efb989e19cb02c1028b34a337e001b146fd1360dc714").Result;
            //var ev2 = coinEv.GetCoinEvent("0xa0876a676d695ab145fcf70ac0b2ae02e8b00351a5193352ffb37ad37dce6848").Result;
            //coinEv.InsertAsync(ev1).Wait();
            //coinEv.InsertAsync(ev2).Wait();
            //var paymentService = ServiceProvider.GetService<ICoinTransactionService>();
            //paymentService.PutTransactionToQueue("0xbfb8d6a561c1a088c347efb989e19cb02c1028b34a337e001b146fd1360dc714").Wait();
            //paymentService.PutTransactionToQueue("0xa0876a676d695ab145fcf70ac0b2ae02e8b00351a5193352ffb37ad37dce6848").Wait();
            //var pendingOperationService = ServiceProvider.GetService<IPendingOperationService>();
            //var op = pendingOperationService.GetOperationAsync("40017691-1656-4d71-a8a6-4187200dca73").Result;
            //pendingOperationService.CreateOperation(op).Wait();
            //var op2 = pendingOperationService.GetOperationAsync("41e19fd5-2660-469b-9315-b768f701e742").Result;
            //pendingOperationService.CreateOperation(op2).Wait();

            while (!exit)
            {
                Console.WriteLine("Choose number: ");
                //Console.WriteLine("1. Deploy main contract from local json file");
                Console.WriteLine("2. Deploy main exchange contract");
                Console.WriteLine("3. Deploy coin contract using local json file");
                Console.WriteLine("4. Deploy transfer");
                Console.WriteLine("5. Deploy BCAP Token");
                Console.WriteLine("6. Deploy main exchange contract with multiple owners!(Make sure that jobs are stopped)");
                Console.WriteLine("7. Add more owners to Main Exchange Contract with multiple owners!(Add addresses with some eth on it)");
                Console.WriteLine("9. Deploy And Migrate To NM!(Make sure that jobs are stopped)");
                Console.WriteLine("10. Send transaction to MainExchange!(Make sure that jobs are stopped)");
                Console.WriteLine("0. Exit");

                var input = Console.ReadLine();

                switch (input)
                {
                    case "2":
                        DeployMainExchangeContract().Wait();
                        break;
                    case "3":
                        DeployCoinContract().Wait();
                        break;
                    case "4":
                        DeployTokenTransferContract().Wait();
                        break;
                    case "0":
                        exit = true;
                        break;
                    case "5":
                        DeployBCAP().Wait();
                        break;
                    case "6":
                        DeployAndMigrateMainExchangeContractWithMultipleOwners().Wait();
                        break;
                    case "7":
                        AddOwners().Wait();
                        break;
                    //case "8":
                    //    MigrateAdapter(,).Wait();
                    //    break;
                    case "9":
                        DeployAndMigrateToNM().Wait();
                        break;
                    case "10":
                        SendTransactionFromMainExchange().Wait();
                        break;
                    default:
                        Console.WriteLine("Bad input!");
                        continue;
                }

                Console.WriteLine("Done!");
            }
        }

        private class EthereumContractExtended
        {
            public string EthereumContractName { get; set; }
            public EthereumContract Contract { get; set; }
        }

        private static void GetAllContractInJson()
        {
            string runningDir = Directory.GetCurrentDirectory();
            var jsonWithAllCompiledContracts = Path.Combine(runningDir, "contracts", "contractSettings.json");
            File.Delete(jsonWithAllCompiledContracts);
            var dirPath = Path.Combine(runningDir, "contracts", "bin");
            var allContracts = Directory.EnumerateFiles(dirPath, @"*.abi");
            var ethereumContracts = new List<EthereumContractExtended>();
            foreach (var contract in allContracts)
            {
                FileInfo fi = new FileInfo(contract);
                string contentAbi = File.ReadAllText(contract);
                string byteCode = File.ReadAllText(contract.Replace(".abi", ".bin"));
                ethereumContracts.Add(new EthereumContractExtended()
                {
                    EthereumContractName = fi.Name,
                    Contract = new EthereumContract()
                    {
                        Abi = contentAbi,
                        ByteCode = byteCode,
                    }
                });
            }

            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(ethereumContracts, Formatting.Indented);
            File.AppendAllText(jsonWithAllCompiledContracts, serialized);
        }

        private static async Task DeployTokenTransferContract()
        {
            Console.WriteLine("Begin tokenTransferContract deployment process");

            try
            {
                string transferName = null;
                bool exit = false;
                Console.WriteLine("Choose transfer contract type:");
                Console.WriteLine("1 - Ethereum transfer contract");
                Console.WriteLine("2 - Token transfer contract");
                while (!exit)
                {

                    var input = Console.ReadLine();
                    switch (input)
                    {
                        case "1":
                            transferName = "TokenTransferContract";
                            exit = true;
                            break;

                        case "2":
                            transferName = "EthTransferContract";
                            exit = true;
                            break;
                        default:
                            break;
                    }
                }

                Console.WriteLine("Write Client Address (you have one attempt):");
                var clientAddress = Console.ReadLine();

                var settings = GetCurrentSettings();

                var abi = GetFileContent($"{transferName}.abi");
                var bytecode = GetFileContent($"{transferName}.bin");

                string contractAddress = await ServiceProvider.GetService<IContractService>().CreateContract(abi, bytecode, clientAddress);
                settings.EthereumCore.TokenTransferContract = new EthereumContract
                {
                    Address = contractAddress,
                    Abi = abi,
                    ByteCode = bytecode
                };
                Console.WriteLine("New contract: " + contractAddress);
                SaveSettings(settings);

                Console.WriteLine("Contract address stored in generalsettings.json file");
            }
            catch (Exception e)
            {
                Console.WriteLine("Action failed!");
                Console.WriteLine(e.Message);
            }
        }

        static async Task DeployMainContractLocal()
        {
            Console.WriteLine("Begin contract deployment process");
            try
            {
                var settings = GetCurrentSettings();

                string contractAddress = await ServiceProvider.GetService<IContractService>().CreateContract(settings.EthereumCore.MainContract.Abi, settings.EthereumCore.MainContract.ByteCode);

                settings.EthereumCore.MainContract.Address = contractAddress;

                Console.WriteLine("New contract: " + contractAddress);

                SaveSettings(settings);

                Console.WriteLine("Contract address stored in generalsettings.json file");
            }
            catch (Exception e)
            {
                Console.WriteLine("Action failed!");
                Console.WriteLine(e.Message);
            }
        }

        static async Task DeployCoinContract()
        {
            string name, path;
            do
            {
                Console.WriteLine("Enter coin name:");
                name = Console.ReadLine();
            } while (string.IsNullOrWhiteSpace(name));
            do
            {
                Console.WriteLine("Enter coin file name:");
                path = Console.ReadLine();
            } while (string.IsNullOrWhiteSpace(path) || !File.Exists(GetFilePath(path + ".abi")));

            Console.WriteLine("Begin coin contract deployment process");
            try
            {
                var abi = GetFileContent(path + ".abi");
                var bytecode = GetFileContent(path + ".bin");
                var settings = GetCurrentSettings();
                string contractAddress = await ServiceProvider.GetService<IContractService>().CreateContract(abi, bytecode, settings.EthereumCore.MainExchangeContract.Address);
                if (settings.EthereumCore.CoinContracts == null)
                    settings.EthereumCore.CoinContracts = new Dictionary<string, EthereumContract>();

                settings.EthereumCore.CoinContracts[name] = new EthereumContract { Address = contractAddress, Abi = abi };

                Console.WriteLine("New coin contract: " + contractAddress);

                SaveSettings(settings);

                Console.WriteLine("Contract address stored in generalsettings.json file");
            }
            catch (Exception e)
            {
                Console.WriteLine("Action failed!");
                Console.WriteLine(e.Message);
            }
        }

        static async Task DeployMainExchangeContract()
        {
            Console.WriteLine("Begin main exchange contract deployment process");
            try
            {
                var settings = GetCurrentSettings();
                var abi = GetFileContent("MainExchange.abi");
                var bytecode = GetFileContent("MainExchange.bin");
                string contractAddress = await ServiceProvider.GetService<IContractService>().CreateContract(abi, bytecode);

                settings.EthereumCore.MainExchangeContract = new Core.Settings.EthereumContract { Abi = abi, ByteCode = bytecode, Address = contractAddress };
                Console.WriteLine("New main exchange contract: " + contractAddress);

                SaveSettings(settings);

                Console.WriteLine("Contract address stored in generalsettings.json file");
            }
            catch (Exception e)
            {
                Console.WriteLine("Action failed!");
                Console.WriteLine(e.Message);
            }
        }

        #region MO

        static async Task DeployAndMigrateMainExchangeContractWithMultipleOwners()
        {
            var abi = GetFileContent("MainExchangeMultipleOwners.abi");
            string newContractAddress = await DeployMainExchangeContractWithMultipleOwners();
            if (string.IsNullOrEmpty(newContractAddress))
            {
                Console.WriteLine("Deploying failed. Can't proceed with migration");

                return;
            }

            await MigrateAdapter(newContractAddress, abi);
        }

        static async Task<string> DeployMainExchangeContractWithMultipleOwners()
        {
            Console.WriteLine("Begin main exchange contract deployment process");
            try
            {
                var settings = GetCurrentSettings();
                var abi = GetFileContent("MainExchangeMultipleOwners.abi");
                var bytecode = GetFileContent("MainExchangeMultipleOwners.bin");
                string contractAddress = await ServiceProvider.GetService<IContractService>().CreateContract(abi, bytecode);
                IBaseSettings baseSettings = ServiceProvider.GetService<IBaseSettings>();
                settings.EthereumCore.MainExchangeContract = new Core.Settings.EthereumContract { Abi = abi, ByteCode = bytecode, Address = contractAddress };
                Console.WriteLine("New main exchange contract: " + contractAddress);

                SaveSettings(settings);

                Console.WriteLine("Contract address stored in generalsettings.json file");

                return contractAddress;
            }
            catch (Exception e)
            {
                Console.WriteLine("Action failed!");
                Console.WriteLine(e.Message);

                return "";
            }
        }

        #endregion

        #region NM

        static async Task DeployAndMigrateToNM()
        {
            var abi = GetFileContent("MainExchangeNM.abi");
            string newContractAddress = await DeployMainExchangeContractNM();
            if (string.IsNullOrEmpty(newContractAddress))
            {
                Console.WriteLine("Deploying failed. Can't proceed with migration");

                return;
            }

            await MigrateAdapter(newContractAddress, abi);
        }

        static async Task<string> DeployMainExchangeContractNM()
        {
            Console.WriteLine("Begin main exchange contract deployment process");
            try
            {
                var settings = GetCurrentSettings();
                var abi = GetFileContent("MainExchangeNM.abi");
                var bytecode = GetFileContent("MainExchangeNM.bin");
                string contractAddress = await ServiceProvider.GetService<IContractService>().CreateContract(abi, bytecode);

                settings.EthereumCore.MainExchangeContract = new Core.Settings.EthereumContract { Abi = abi, ByteCode = bytecode, Address = contractAddress };
                Console.WriteLine("New main exchange contract: " + contractAddress);

                SaveSettings(settings);
                Console.WriteLine("Contract address stored in generalsettings.json file");

                return contractAddress;
            }
            catch (Exception e)
            {
                Console.WriteLine("Action failed!");
                Console.WriteLine(e.Message);

                return "";
            }
        }

        static async Task SendTransactionFromMainExchange()
        {
            string operationId = "";
            IPendingOperationService pendingOperationService = ServiceProvider.GetService<IPendingOperationService>();
            try
            {
                MonitoringOperationJob job = ServiceProvider.GetService<MonitoringOperationJob>();
                IExchangeContractService exchangeContractService = ServiceProvider.GetService<IExchangeContractService>();
                string filePath = Path.Combine(AppContext.BaseDirectory, "transferTransaction.txt");
                var content = File.ReadAllText(filePath);
                TransferModel model = Newtonsoft.Json.JsonConvert.DeserializeObject<TransferModel>(content);
                var addressUtil = new AddressUtil();
                BigInteger amount = BigInteger.Parse(model.Amount);
                operationId = await pendingOperationService.TransferWithNoChecks(model.Id, model.CoinAdapterAddress,
                    addressUtil.ConvertToChecksumAddress(model.FromAddress), addressUtil.ConvertToChecksumAddress(model.ToAddress), amount, model.Sign);

                Console.WriteLine($"OperationId - {operationId}");
                await job.ProcessOperation(new Services.New.Models.OperationHashMatchMessage()
                {
                    OperationId = operationId,
                },null, exchangeContractService.TransferWithoutSignCheck);

                Console.WriteLine("Start removing from processing queue");
                await pendingOperationService.RemoveFromPendingOperationQueue(operationId);
                Console.WriteLine("Stop removing from processing queue");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message} - {e.StackTrace}");
                Console.WriteLine("Start removing from processing queue");
                await pendingOperationService.RemoveFromPendingOperationQueue(operationId);
                Console.WriteLine("Stop removing from processing queue");
            }
        }
        #endregion

        static async Task MigrateAdapter(string mainExchangeAddress, string mainExchangeAbi)
        {
            Console.WriteLine("Begin ethereum adapter migration process");
            try
            {
                //Console.WriteLine("Type new main exchange address:");
                //string newMainExchangeAddress = Console.ReadLine().Trim().ToLower();
                var settings = GetCurrentSettings();
                var exchangeService = ServiceProvider.GetService<IExchangeContractService>();
                var ethereumTransactionService = ServiceProvider.GetService<IEthereumTransactionService>();
                IEnumerable<ICoin> adapters = await ServiceProvider.GetService<ICoinRepository>().GetAll();
                foreach (var adapter in adapters)
                {
                    string transactionHash = await exchangeService.ChangeMainContractInCoin(adapter.AdapterAddress,
                        mainExchangeAddress, mainExchangeAbi);
                    Console.WriteLine($"Coin adapter: {adapter.AdapterAddress} - reassign main exchange {transactionHash}");

                    while (!await ethereumTransactionService.IsTransactionExecuted(transactionHash, Constants.GasForCoinTransaction))
                    {
                        await Task.Delay(400);
                    }
                }

                IBaseSettings baseSettings = ServiceProvider.GetService<IBaseSettings>();
                baseSettings.MainExchangeContract.Address = mainExchangeAddress;
                baseSettings.MainExchangeContract.Abi = mainExchangeAbi;

                Console.WriteLine("Coin adapters has been migrated");
            }
            catch (Exception e)
            {
                Console.WriteLine("Action failed!");
                Console.WriteLine(e.Message);
            }
        }

        static async Task AddOwners()
        {
            Console.WriteLine("Begin adding owners to main exchange process");
            try
            {
                var settings = GetCurrentSettings();
                var exchangeService = ServiceProvider.GetService<IExchangeContractService>();
                var ownerService = ServiceProvider.GetService<IOwnerService>();
                IBaseSettings baseSettings = ServiceProvider.GetService<IBaseSettings>();
                //baseSettings.MainExchangeContract.Address = "0xf5f0f53f86b7a5a92f150b1cf0edc12969b51f7e";
                baseSettings.MainExchangeContract.Abi = GetFileContent("MainExchangeMultipleOwners.abi");

                Console.WriteLine("Put public addressses below, type -1 to commit owners to main exchange");
                string input = "";
                List<IOwner> owners = new List<IOwner>();
                while (input != "-1")
                {
                    input = Console.ReadLine();
                    if (exchangeService.IsValidAddress(input))
                    {
                        owners.Add(new Owner()
                        {
                            Address = input
                        });
                    }
                    else if (input != "-1")
                    {
                        Console.WriteLine($"{input} is not a valid address");
                    }
                }
                Console.WriteLine("Start commiting transaction!");
                await ownerService.AddOwners(owners);
                Console.WriteLine("Owners were added successfuly!");

                Console.WriteLine("Completed");
            }
            catch (Exception e)
            {
                Console.WriteLine("Action failed!");
                Console.WriteLine(e.Message);
            }
        }

        static async Task DeployBCAP()
        {
            Console.WriteLine("Begin BCAP deployment process");
            try
            {
                var settings = GetCurrentSettings();
                var abi = GetFileContent("BCAPToken.abi");
                var bytecode = GetFileContent("BCAPToken.bin");
                string contractAddress = await ServiceProvider.GetService<IContractService>().CreateContract(abi, bytecode, settings.EthereumCore.EthereumMainAccount);

                settings.EthereumCore.MainExchangeContract = new EthereumContract { Abi = abi, ByteCode = bytecode, Address = contractAddress };
                Console.WriteLine("New BCAP Token: " + contractAddress);

                SaveSettings(settings);

                Console.WriteLine("Contract address stored in generalsettings.json file");
            }
            catch (Exception e)
            {
                Console.WriteLine("Action failed!");
                Console.WriteLine(e.Message);
            }
        }

        static SettingsWrapper GetCurrentSettingsFromUrl()
        {
            FileInfo fi = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
            var location = Path.Combine(fi.DirectoryName, "..", "..", "..");
            var builder = new ConfigurationBuilder()
                .SetBasePath(location)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            var configuration = builder.Build();
            var connString = configuration.GetConnectionString("ConnectionString");
            var settings = GeneralSettingsReader.ReadGeneralSettings<SettingsWrapper>(connString);

            return settings;
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
            var connString = configuration.GetConnectionString("ConnectionString");
            var path = GetSettingsPath();
            var settings = GeneralSettingsReader.ReadGeneralSettingsLocal<SettingsWrapper>(path);

            return settings;
        }

        static void SaveSettings(SettingsWrapper settings)
        {
            File.WriteAllText(GetSettingsPath(), JsonConvert.SerializeObject(settings, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }

        static string GetFilePath(string fileName)
        {
            return Path.Combine("contracts", "bin", fileName);
        }

        static string GetFileContent(string fileName)
        {
            return File.ReadAllText(GetFilePath(fileName));
        }

        static string GetSettingsPath()
        {
            return "generalsettings.json";
        }

        static void WriteEventsForContract()
        {
            Web3 client = new Web3("http://localhost:8000");

            var contract = client.Eth.GetContract("[{\"constant\":false,\"inputs\":[],\"name\":\"kill\",\"outputs\":[],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"id\",\"type\":\"uint256\"},{\"name\":\"coin\",\"type\":\"address\"},{\"name\":\"receiver\",\"type\":\"address\"},{\"name\":\"amount\",\"type\":\"uint256\"},{\"name\":\"gas\",\"type\":\"uint256\"},{\"name\":\"params\",\"type\":\"bytes\"}],\"name\":\"cashin\",\"outputs\":[],\"payable\":false,\"type\":\"function\"},{\"inputs\":[{\"name\":\"userAddress\",\"type\":\"address\"},{\"name\":\"coinAdapterAddress\",\"type\":\"address\"}],\"payable\":false,\"type\":\"constructor\"},{\"payable\":true,\"type\":\"fallback\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":false,\"name\":\"_eventNumber\",\"type\":\"int256\"},{\"indexed\":false,\"name\":\"_value\",\"type\":\"string\"}],\"name\":\"DebugEvent\",\"type\":\"event\"}]\n"
                , "0xe3a61032878b47b12403e89241656d78a9201f9d");
            var @event = contract.GetEvent("DebugEvent");
            //var lastBlock = client.Eth.Blocks.GetBlockNumber.SendRequestAsync().Result;
            //var previousBlock = (ulong)(lastBlock.Value - 40000);

            var allEvents = @event.GetAllChanges<DebugEvent>(new Nethereum.RPC.Eth.DTOs.NewFilterInput()
            {
                FromBlock = new Nethereum.RPC.Eth.DTOs.BlockParameter(849740),
                ToBlock = new Nethereum.RPC.Eth.DTOs.BlockParameter(849745)
            }).Result;

            foreach (var item in allEvents)
            {
                Console.WriteLine($"{item.Event.EventNumber} {item.Event.Value}");
            }
        }
    }
}

//eth.sendTransaction({from:eth.coinbase, to:0xdd436bac8e76da5a9a73b1999c319dde2da85f8d, value: web3.toWei(0.05, "ether")}) 
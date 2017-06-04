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

            var settings = GetCurrentSettings();

            IServiceCollection collection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            collection.AddSingleton<IBaseSettings>(settings.EthereumCore);
            collection.AddSingleton<ISlackNotificationSettings>(settings.SlackNotifications);

            RegisterReposExt.RegisterAzureLogs(collection, settings.EthereumCore, "");
            RegisterReposExt.RegisterAzureQueues(collection, settings.EthereumCore, settings.SlackNotifications);
            RegisterReposExt.RegisterAzureStorages(collection, settings.EthereumCore, settings.SlackNotifications);
            RegisterDependency.RegisterServices(collection);
            //var key = EthECKey.GenerateKey().GetPrivateKeyAsBytes();
            //var stringKey = Encoding.Unicode.GetString(key);
            GetAllContractInJson();
            ServiceProvider = collection.BuildServiceProvider();

            //var lykkeSigningAPI = ServiceProvider.GetService<ILykkeSigningAPI>();
            //lykkeSigningAPI.ApiEthereumAddkeyPost(new AddKeyRequest()
            //{
            //    Key = "",
            //});

            //var service = ServiceProvider.GetService<IErcInterfaceService>();
            //service.Transfer("0xce2ef46ecc168226f33b6f6b8a56e90450d0d2c0", settings.EthereumMainAccount,
            //    "0x6e95184c02c39369ee9449f85aee42badc6910fd", new System.Numerics.BigInteger(101)).Wait();
            //var paymentService = ServiceProvider.GetService<IPaymentService>();
            //string result = paymentService.SendEthereum(settings.EthereumMainAccount, 
            //    "0xbb0a9c08030898cdaf1f28633f0d3c8556155482", new System.Numerics.BigInteger(5000000000000000)).Result;

            while (!exit)
            {
                Console.WriteLine("Choose number: ");
                //Console.WriteLine("1. Deploy main contract from local json file");
                Console.WriteLine("2. Deploy main exchange contract");
                Console.WriteLine("3. Deploy coin contract using local json file");
                Console.WriteLine("4. Deploy transfer");
                Console.WriteLine("5. Deploy BCAP Token");
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
                SaveSettings(settings.EthereumCore);

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

                SaveSettings(settings.EthereumCore);

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

                SaveSettings(settings.EthereumCore);

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

                settings.EthereumCore.MainExchangeContract = new EthereumContract { Abi = abi, ByteCode = bytecode, Address = contractAddress };
                Console.WriteLine("New main exchange contract: " + contractAddress);

                SaveSettings(settings.EthereumCore);

                Console.WriteLine("Contract address stored in generalsettings.json file");
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

                SaveSettings(settings.EthereumCore);

                Console.WriteLine("Contract address stored in generalsettings.json file");
            }
            catch (Exception e)
            {
                Console.WriteLine("Action failed!");
                Console.WriteLine(e.Message);
            }
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

        static void SaveSettings(BaseSettings settings)
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
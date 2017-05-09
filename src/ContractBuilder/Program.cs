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

        public static void Main(string[] args)
        {
            var exit = false;

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            var configuration = configurationBuilder.Build();

            var settings = GetCurrentSettings();

            //var key = EthECKey.GenerateKey().GetPrivateKeyAsBytes();
            //var stringKey = Encoding.Unicode.GetString(key);
            GetAllContractInJson();
            //var service = new ErcInterfaceService(settings);
            //service.Transfer("0xbefc091843a4c958ec929c3b90622fb6c3fce3e9", settings.EthereumMainAccount,
            //    "0x13415ca1cd099a837ef264873c03bf4b8e8e1f39", new System.Numerics.BigInteger(999)).Wait();
            

            while (!exit)
            {
                Console.WriteLine("Choose number: ");
                //Console.WriteLine("1. Deploy main contract from local json file");
                Console.WriteLine("2. Deploy main exchange contract using local json file");
                Console.WriteLine("3. Deploy coin contract using local json file");
                Console.WriteLine("4. Deploy transfer using local json file");
                Console.WriteLine("0. Exit");

                var input = Console.ReadLine();

                switch (input)
                {
                    //case "1":
                    //    DeployMainContractLocal().Wait();
                    //    break;
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

                string contractAddress = await new ContractService(settings, null, new Web3(settings.EthereumUrl)).CreateContract(abi, bytecode, clientAddress);
                settings.TokenTransferContract = new EthereumContract
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

                string contractAddress = await new ContractService(settings, null, new Web3(settings.EthereumUrl)).CreateContract(settings.MainContract.Abi, settings.MainContract.ByteCode);

                settings.MainContract.Address = contractAddress;

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
                string contractAddress = await new ContractService(settings, null, new Web3(settings.EthereumUrl)).CreateContract(abi, bytecode, settings.MainExchangeContract.Address);
                if (settings.CoinContracts == null)
                    settings.CoinContracts = new Dictionary<string, EthereumContract>();

                settings.CoinContracts[name] = new EthereumContract { Address = contractAddress, Abi = abi };

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
                string contractAddress = await new ContractService(settings, null, new Web3(settings.EthereumUrl)).CreateContract(abi, bytecode);

                settings.MainExchangeContract = new EthereumContract { Abi = abi, ByteCode = bytecode, Address = contractAddress };
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

        static BaseSettings GetCurrentSettings()
        {
            var json = File.ReadAllText(GetSettingsPath());
            var settings = JsonConvert.DeserializeObject<BaseSettings>(json);
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
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Settings;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Lykke.Service.EthereumCore.Services;
using Nethereum.Web3;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Text;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Service.EthereumCore.AzureRepositories;
using SigningServiceApiCaller;
using SigningServiceApiCaller.Models;
using Lykke.Service.EthereumCore.Services.Coins;
using RabbitMQ;
using Common.Log;
using Lykke.Service.EthereumCore.Services.New;
//using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Models;
using System.Numerics;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.Service.EthereumCore.Core.Repositories;
using Nethereum.Util;
using Lykke.Job.EthereumCore.Job;
using Lykke.Service.EthereumCore.Core.PrivateWallet;
using Lykke.Service.EthereumCore.Core.Services;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using Lykke.Service.EthereumCore.Services.Signature;
using EthereumContract = Lykke.Service.EthereumCore.Core.Settings.EthereumContract;
using Lykke.Service.RabbitMQ;
using Lykke.SettingsReader;
using Nethereum.Hex.HexTypes;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;

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
        public static IContainer ServiceProvider { get; set; }

        public static void Main(string[] args)
        {
            if (args == null || !args.Any())
            {
                throw new Exception("Specify parameters! url to EtherteumCore service");
            }

            var exit = false;

            string settingsArg = args.First();

            var settings = new SettingsServiceReloadingManager<AppSettings>(settingsArg);
            SaveSettings(settings);

            ContainerBuilder containerBuilder = new ContainerBuilder();
            IServiceCollection collection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            containerBuilder.RegisterInstance(settings);
            containerBuilder.RegisterInstance<IBaseSettings>(settings.CurrentValue.EthereumCore);
            containerBuilder.RegisterInstance<ISlackNotificationSettings>(settings.CurrentValue.SlackNotifications);
            containerBuilder.RegisterInstance(settings.Nested(x => x.EthereumCore));
            containerBuilder.RegisterInstance(settings.CurrentValue);
            var consoleLogger = new LogToConsole();
            collection.AddSingleton<ILog>(consoleLogger);

            //TODO: Uncomment and fix registrations
            RegisterReposExt.RegisterAzureQueues(containerBuilder, settings.Nested(x => x.EthereumCore.Db.DataConnString),
                settings.Nested(x => x.SlackNotifications));
            RegisterReposExt.RegisterAzureStorages(containerBuilder, settings.Nested(x => x.EthereumCore),
                settings.Nested(x => x.SlackNotifications), consoleLogger);
            RegisterRabbitQueueEx.RegisterRabbitQueue(collection,
                settings.Nested(x => x.EthereumCore.RabbitMq),
                settings.Nested(x => x.EthereumCore.Db.DataConnString),
                consoleLogger);
            RegisterDependency.RegisterServices(collection);
            RegisterDependency.RegisterServices(containerBuilder);
            //Lykke.Job.EthereumCore.Config.RegisterDependency.RegisterJobs(collection);
            //var web3 = ServiceProvider.GetService<Web3>();
            //web3.Eth.GetBalance.SendRequestAsync("");
            // web3.Eth.Transactions.SendTransaction.SendRequestAsync(new Nethereum.RPC.Eth.DTOs.TransactionInput()
            //{
            //    
            //}).Result;
            //var key = EthECKey.GenerateKey().GetPrivateKeyAsBytes();
            //var stringKey = Encoding.Unicode.GetString(key);
            GetAllContractInJson();
            containerBuilder.Populate(collection);
            ServiceProvider = containerBuilder.Build();
            ServiceProvider.ActivateRequestInterceptor();
            var erc20PrivateWalletService = ServiceProvider.Resolve<IErc20PrivateWalletService>();
            var estimationService = ServiceProvider.Resolve<IEstimationService>();
            var nonceCalculator = ServiceProvider.Resolve<INonceCalculator>();
            var rx1 = nonceCalculator.GetNonceAsync("0x6Da4B01c3A88d67Fddc2Cb8B5505723409114b45", true).Result;


            #region Estimation

            //var estimation2 = estimationService.EstimateTransactionExecutionCostAsync(
            //    "0x1015b65d76E686a3001Fc0a42a91F058edf067Ad"
            //    , "0x6303F9f7f1C57D0fF48fE6baD5161967f58de8fa"
            //    , BigInteger.Parse("1000000000000000000")
            //    , BigInteger.Parse("1000000000"),
            //    null).Result;

            //var estimationFullAmountX = estimationService.EstimateTransactionExecutionCostAsync(
            //    "0xC4f72f0cc3807Bf9DdF16D400Eafc321a0C38a72"
            //    , "0xbdfeff9a1f4a1bdf483d680046344316019c58cf"
            //    , BigInteger.Parse("8052415760625306547")
            //    , BigInteger.Parse("1000000000"),
            //    null).Result;

            //var estimationFullAmount1 = estimationService.EstimateTransactionExecutionCostAsync(
            //    "0x1015b65d76E686a3001Fc0a42a91F058edf067Ad"
            //    , "0xbdfeff9a1f4a1bdf483d680046344316019c58cf"
            //    , BigInteger.Parse("8052415760625306547")
            //    , BigInteger.Parse("1000000000"),
            //    null).Result;

            //var estimationFullAmountContract = estimationService.EstimateTransactionExecutionCostAsync(
            //         "0x1015b65d76E686a3001Fc0a42a91F058edf067Ad"
            //        , "0x40c914c3A8Fc4c3ccC1d3682828928515DFa631b"
            //        , BigInteger.Parse("8052415760625306547")
            //        , BigInteger.Parse("1000000000"),
            //        null).Result;

            //var estimation = estimationService.EstimateTransactionExecutionCostAsync(
            //    "0x1015b65d76E686a3001Fc0a42a91F058edf067Ad"
            //    , "0xbdfeff9a1f4a1bdf483d680046344316019c58cf"
            //    , BigInteger.Parse("1000000000000000000")
            //    , BigInteger.Parse("1000000000"),
            //    null).Result;

            //var estimation1 = estimationService.EstimateTransactionExecutionCostAsync(
            //    "0x1015b65d76E686a3001Fc0a42a91F058edf067Ad"
            //    , "0x6303F9f7f1C57D0fF48fE6baD5161967f58de8fa"
            //    , BigInteger.Parse("1000000000000000000")
            //    , BigInteger.Parse("1000000000"),
            //    null).Result;

            //var estimation2 = estimationService.EstimateTransactionExecutionCostAsync(
            //    "0x1015b65d76E686a3001Fc0a42a91F058edf067Ad"
            //    , "0xe152a70E09661c248A8e4B883CF8288670785795"
            //    , BigInteger.Parse("1000000000000000000")
            //    , BigInteger.Parse("1000000000"),
            //    null).Result;

            //var estimation3 = estimationService.EstimateTransactionExecutionCostAsync(
            //    "0x1015b65d76E686a3001Fc0a42a91F058edf067Ad"
            //    , "0xdBe3A455Ae330645D931817B7440b1C4f6DcF549"
            //    , BigInteger.Parse("1000000000000000000")
            //    , BigInteger.Parse("1000000000"),
            //    null).Result;

            //var estimation4 = estimationService.EstimateTransactionExecutionCostAsync(
            //    "0x1015b65d76E686a3001Fc0a42a91F058edf067Ad"
            //    , "0x40c914c3A8Fc4c3ccC1d3682828928515DFa631b"
            //    , BigInteger.Parse("1000000000000000000")
            //    , BigInteger.Parse("1000000000"),
            //    null).Result;

            //var estimation5 = estimationService.EstimateTransactionExecutionCostAsync(
            //    "0x1015b65d76E686a3001Fc0a42a91F058edf067Ad"
            //    , "0xF9404188b2A53E6a29dA9F960eC318c452E38E3c"
            //    , BigInteger.Parse("1000000000000000000")
            //    , BigInteger.Parse("1000000000"),
            //    null).Result;

            #region Erc20

            var data6 = erc20PrivateWalletService.GetTransferFunctionCallEncoded(
                "0x5adbf411faf2595698d80b7f93d570dd16d7f4b2",
                "0xbdfeff9a1f4a1bdf483d680046344316019c58cf",
                BigInteger.Parse("1000000000000000000"));
            var estimation6 = estimationService.EstimateTransactionExecutionCostAsync(
                "0x406561f72e25af10fd28b41200fa3d52badc5a21"
                , "0x5adbf411faf2595698d80b7f93d570dd16d7f4b2"
                , BigInteger.Zero
                , BigInteger.Parse("1000000000"),
                data6).Result;

            var data7 = erc20PrivateWalletService.GetTransferFunctionCallEncoded(
                "0x5adbf411faf2595698d80b7f93d570dd16d7f4b2",
                "0xbdfeff9a1f4a1bdf483d680046344316019c58cf",
                BigInteger.Parse("997781905042671000000000000000000"));

            var estimation7 = estimationService.EstimateTransactionExecutionCostAsync(
                "0x406561f72e25af10fd28b41200fa3d52badc5a21"
                , "0x5adbf411faf2595698d80b7f93d570dd16d7f4b2"
                , BigInteger.Zero
                , BigInteger.Parse("1000000000"),
                data7).Result;

            var data8 = erc20PrivateWalletService.GetTransferFunctionCallEncoded(
                "0x5adbf411faf2595698d80b7f93d570dd16d7f4b2",
                "0xbdfeff9a1f4a1bdf483d680046344316019c58cf",
                BigInteger.Parse("9977819050426710000000000000000000"));

            var estimation8 = estimationService.EstimateTransactionExecutionCostAsync(
                "0x406561f72e25af10fd28b41200fa3d52badc5a21"
                , "0x5adbf411faf2595698d80b7f93d570dd16d7f4b2"
                , BigInteger.Zero
                , BigInteger.Parse("1000000000"),
                data8).Result;

            #endregion

            #endregion


            //var web3 = ServiceProvider.Resolve<IWeb3>();
            //var abi = GetFileContent("LuCyToken.abi");
            //var bytecode = GetFileContent("LuCyToken.bin");
            //var tokenAddress = "0x16e17f0e2c645b21304dcf362c58d2060d34dad9";
            //var contract = web3.Eth.GetContract(abi, tokenAddress);

            //var getTermsAndConditionsFunc = contract.GetFunction("getTermsAndConditions");
            //var setTermsAndConditionsFunc = contract.GetFunction("setTermsAndConditions");
            //var tC =  getTermsAndConditionsFunc.CallAsync<string>().Result;
            //var transactionHash = setTermsAndConditionsFunc.SendTransactionAsync(settings.CurrentValue.EthereumCore.EthereumMainAccount,
            //        new HexBigInteger(BigInteger.Parse("200000")), new HexBigInteger(0), "https://www.lykke.com/cp/terms_of_use").Result;


            //var signApi = ServiceProvider.Resolve<ILykkeSigningAPI>();
            //var signed = signApi.ApiEthereumSignPostWithHttpMessagesAsync(new EthereumTransactionSignRequest()
            //{
            //    FromProperty = "0x406561F72e25af10fD28b41200FA3D52badC5A21",
            //    Transaction = "f86b8201618483215600830186a094db6ea754f2afb4257b9bea0828771b19504aeac380b844a9059cbb00000000000000000000000033c6b8907d65d5a53cc301c544abf99f2e783242000000000000000000000000000000000000000000000000000000000000000c808080"
            //}).Result;
            //var xa = 1;
            //var lykkeSigningAPI = ServiceProvider.Resolve<ILykkeSigningAPI>();
            //var key1 = lykkeSigningAPI.ApiEthereumKeyGetAsync().Result;

            //var eventService = ServiceProvider.GetService<ITransactionEventsService>();
            //eventService.IndexCashinEventsForAdapter("0x1c4ca817d1c61f9c47ce2bec9d7106393ff981ce",
            //    "0x512867d36f1d6ee43f2056a7c41606133bce514fbc8e911c1834eeae80800ceb").Wait();

            //#region EmissiveErc223 TOKEN

            //string tokenAddress = "";
            //string depositAddress = "";
            //Contract contract;

            //var web3 = ServiceProvider.Resolve<IWeb3>();
            //{
            //    var abi = GetFileContent("Erc20DepositContract.abi");
            //    var bytecode = GetFileContent("Erc20DepositContract.bin");
            //    depositAddress = string.IsNullOrEmpty(depositAddress) ?
            //        ServiceProvider.Resolve<IContractService>()
            //        .CreateContract(abi,
            //                bytecode,
            //                4000000)
            //        .Result : depositAddress;
            //}
            //{
            //    //address issuer,
            //    //string tokenName,
            //    //uint8 divisibility,
            //    //string tokenSymbol,
            //    //string version
            //    var abi = GetFileContent("EmissiveErc223Token.abi");
            //    var bytecode = GetFileContent("EmissiveErc223Token.bin");
            //    tokenAddress = string.IsNullOrEmpty(tokenAddress) ?
            //        ServiceProvider.Resolve<IContractService>()
            //        .CreateContract(abi,
            //                bytecode,
            //                4000000,
            //                settings.CurrentValue.EthereumCore.EthereumMainAccount,
            //                "LykkeErc223Token",
            //                18,
            //                "LTE223",
            //                "1.0.0")
            //        .Result : tokenAddress;
            //    contract = web3.Eth.GetContract(abi, tokenAddress);
            //}

            //{
            //    //Transfer to the deposit contract
            //    var erc20Service = ServiceProvider.Resolve<IErcInterfaceService>();
            //    var balanceOld = erc20Service.GetBalanceForExternalTokenAsync(depositAddress, tokenAddress).Result;
            //    var transactionHash = erc20Service.Transfer(tokenAddress, settings.CurrentValue.EthereumCore.EthereumMainAccount,
            //        depositAddress, System.Numerics.BigInteger.Parse("1000000000000000000")).Result;
            //    WaitForTransactionCompleation(web3, transactionHash);
            //    var balance = erc20Service.GetBalanceForExternalTokenAsync(depositAddress, tokenAddress).Result;
            //    var isPossibleToWithdrawWithTokenFallback = erc20Service.CheckTokenFallback(depositAddress).Result;
            //    var isPossibleToWithdrawToExternal = 
            //        erc20Service.CheckTokenFallback("0x856924997fa22efad8dc75e83acfa916490989a4").Result;
            //}

            //{
            //    //Transfer to the account managed by external private key
            //    var toAddress = "0x856924997fa22efad8dc75e83acfa916490989a4";
            //    var erc20Service = ServiceProvider.Resolve<IErcInterfaceService>();
            //    var balanceOld = erc20Service.GetBalanceForExternalTokenAsync(toAddress, tokenAddress).Result;
            //    var transactionHash = erc20Service.Transfer(tokenAddress, settings.CurrentValue.EthereumCore.EthereumMainAccount,
            //        toAddress, System.Numerics.BigInteger.Parse("1000000000000000000")).Result;
            //    WaitForTransactionCompleation(web3, transactionHash);
            //    var balance = erc20Service.GetBalanceForExternalTokenAsync(toAddress, tokenAddress).Result;
            //}

            //{
            //    //Transfer to the contract without fallback function
            //    string contractWithoutFallback = "0xd6ff42fa358403e0f9462c08e78c4baea1093945";
            //    var erc20Service = ServiceProvider.Resolve<IErcInterfaceService>();
            //    var balanceOld = erc20Service.GetBalanceForExternalTokenAsync(contractWithoutFallback, tokenAddress).Result;
            //    var transactionHash = erc20Service.Transfer(tokenAddress, settings.CurrentValue.EthereumCore.EthereumMainAccount,
            //        contractWithoutFallback, System.Numerics.BigInteger.Parse("1000000000000000000")).Result;
            //    WaitForTransactionCompleation(web3, transactionHash);
            //    var balance = erc20Service.GetBalanceForExternalTokenAsync(contractWithoutFallback, tokenAddress).Result;
            //    var isPossibleToWithdrawWithoutTokenFallback = erc20Service.CheckTokenFallback(contractWithoutFallback).Result;
            //}

            //#endregion

            //#region DBE TOKEN

            //{
            //    //var abi = GetFileContent("Erc20DepositContract.abi");
            //    //var bytecode = GetFileContent("Erc20DepositContract.bin");
            //    //depositAddress =
            //    //    ServiceProvider.Resolve<IContractService>()
            //    //    .CreateContract(abi, bytecode, 4000000)
            //    //    .Result;
            //}
            //{


            //    var abi = GetFileContent("debtoken.abi");
            //    var bytecode = GetFileContent("debtoken.bin");
            //    //tokenAddress =
            //    //    ServiceProvider.Resolve<IContractService>()
            //    //    .CreateContract(abi, bytecode, 4000000)
            //    //    .Result;
            //    contract = web3.Eth.GetContract(abi, tokenAddress);
            //}

            //{
            //    //var unfreezeFunc = contract.GetFunction("unfreeze");
            //    //var transactionHash = unfreezeFunc.SendTransactionAsync(settings.CurrentValue.EthereumCore.EthereumMainAccount,
            //    //            new HexBigInteger(BigInteger.Parse("200000")), new HexBigInteger(0)).Result;
            //}

            //{
            //    var erc20Service = ServiceProvider.Resolve<IErcInterfaceService>();
            //    var transactionHash = erc20Service.Transfer(tokenAddress, settings.CurrentValue.EthereumCore.EthereumMainAccount,
            //        depositAddress, System.Numerics.BigInteger.Parse("1000000000000000000")).Result;
            //}


            //#endregion

            //#region StatusExamples
            ////var service = ServiceProvider.GetService<ICoinTransactionService>();
            ////{
            ////    //fail
            ////    var x = service.ProcessTransaction(new Services.Coins.Models.CoinTransactionMessage()
            ////    {
            ////        TransactionHash = "0xf86efe1b8de285b8255519ca7d0ac76088132e6c5306f88dfc27312c6d7127ea",
            ////    }).Result;
            ////}

            ////{
            ////    //ok
            ////    var x = service.ProcessTransaction(new Services.Coins.Models.CoinTransactionMessage()
            ////    {
            ////        TransactionHash = "0xa237230df97a0d6710241597a0186662928afa373c13b8d4eac86f36aa678985",
            ////    }).Result;
            ////}

            ////{
            ////    //fail
            ////    var x = service.ProcessTransaction(new Services.Coins.Models.CoinTransactionMessage()
            ////    {
            ////        TransactionHash = "0xb63ac4f94006cbbfe58a1d651e173c56dc74a45e4d1141ac57fc51a0d4202e95",
            ////    }).Result;
            ////}

            ////{
            ////    //fail
            ////    var x = service.ProcessTransaction(new Services.Coins.Models.CoinTransactionMessage()
            ////    {
            ////        TransactionHash = "0x1df50ee79d0af8b433f7f0be2a84cbb5dc3e29e5822e78b9c6a7ec33d027e286",
            ////    }).Result;
            ////}

            ////{
            ////    //fail
            ////    var x = service.ProcessTransaction(new Services.Coins.Models.CoinTransactionMessage()
            ////    {
            ////        TransactionHash = "0xa3d4c1da523273371fe45c928b9236b353976e7b9e6d2b31e659f7a4c781a764",
            ////    }).Result;
            ////}

            //#endregion

            //0xf86efe1b8de285b8255519ca7d0ac76088132e6c5306f88dfc27312c6d7127ea      0x0 
            //0xa237230df97a0d6710241597a0186662928afa373c13b8d4eac86f36aa678985      0x1
            //0xb63ac4f94006cbbfe58a1d651e173c56dc74a45e4d1141ac57fc51a0d4202e95

            //var service = ServiceProvider.Resolve<IErcInterfaceService>();
            //service.Transfer("0x5adbf411faf2595698d80b7f93d570dd16d7f4b2", settings.CurrentValue.EthereumCore.EthereumMainAccount,
            //    "0xae4d8b0c887508750ddb6b32752a82431941e2e7", System.Numerics.BigInteger.Parse("10000000000000000000")).Wait();
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
                        //DeployCoinContract().Wait();
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

                string contractAddress = await ServiceProvider.Resolve<IContractService>().CreateContract(abi, bytecode, 2000000, clientAddress);
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

                string contractAddress = await ServiceProvider.Resolve<IContractService>().CreateContract(settings.EthereumCore.MainContract.Abi, settings.EthereumCore.MainContract.ByteCode);

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

        static async Task DeployMainExchangeContract()
        {
            Console.WriteLine("Begin main exchange contract deployment process");
            try
            {
                var settings = GetCurrentSettings();
                var abi = GetFileContent("MainExchange.abi");
                var bytecode = GetFileContent("MainExchange.bin");
                string contractAddress = await ServiceProvider.Resolve<IContractService>().CreateContract(abi, bytecode);

                settings.EthereumCore.MainExchangeContract = new Lykke.Service.EthereumCore.Core.Settings.EthereumContract { Abi = abi, ByteCode = bytecode, Address = contractAddress };
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
                string contractAddress = await ServiceProvider.Resolve<IContractService>().CreateContract(abi, bytecode);
                IBaseSettings baseSettings = ServiceProvider.Resolve<IBaseSettings>();
                settings.EthereumCore.MainExchangeContract = new Lykke.Service.EthereumCore.Core.Settings.EthereumContract { Abi = abi, ByteCode = bytecode, Address = contractAddress };
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
                string contractAddress = await ServiceProvider.Resolve<IContractService>().CreateContract(abi, bytecode);

                settings.EthereumCore.MainExchangeContract = new Lykke.Service.EthereumCore.Core.Settings.EthereumContract { Abi = abi, ByteCode = bytecode, Address = contractAddress };
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
            IPendingOperationService pendingOperationService = ServiceProvider.Resolve<IPendingOperationService>();
            try
            {
                MonitoringOperationJob job = ServiceProvider.Resolve<MonitoringOperationJob>();
                IExchangeContractService exchangeContractService = ServiceProvider.Resolve<IExchangeContractService>();
                string filePath = Path.Combine(AppContext.BaseDirectory, "transferTransaction.txt");
                var content = File.ReadAllText(filePath);
                TransferModel model = Newtonsoft.Json.JsonConvert.DeserializeObject<TransferModel>(content);
                var addressUtil = new AddressUtil();
                BigInteger amount = BigInteger.Parse(model.Amount);
                operationId = await pendingOperationService.TransferWithNoChecks(model.Id, model.CoinAdapterAddress,
                    addressUtil.ConvertToChecksumAddress(model.FromAddress), addressUtil.ConvertToChecksumAddress(model.ToAddress), amount, model.Sign);

                Console.WriteLine($"OperationId - {operationId}");
                await job.ProcessOperation(new Lykke.Service.EthereumCore.Services.New.Models.OperationHashMatchMessage()
                {
                    OperationId = operationId,
                }, null, exchangeContractService.TransferWithoutSignCheck);

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
                var settings = GetCurrentSettings();
                var exchangeService = ServiceProvider.Resolve<IExchangeContractService>();
                var ethereumTransactionService = ServiceProvider.Resolve<IEthereumTransactionService>();
                IEnumerable<ICoin> adapters = await ServiceProvider.Resolve<ICoinRepository>().GetAll();

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

                IBaseSettings baseSettings = ServiceProvider.Resolve<IBaseSettings>();
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
                var exchangeService = ServiceProvider.Resolve<IExchangeContractService>();
                var ownerService = ServiceProvider.Resolve<IOwnerService>();
                IBaseSettings baseSettings = ServiceProvider.Resolve<IBaseSettings>();
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
                string contractAddress = await ServiceProvider.Resolve<IContractService>().CreateContract(abi, bytecode, 2000000, settings.EthereumCore.EthereumMainAccount);

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

        static IReloadingManager<AppSettings> GetCurrentSettingsFromUrl()
        {
            FileInfo fi = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
            var location = Path.Combine(fi.DirectoryName);
            var builder = new ConfigurationBuilder()
                .SetBasePath(location)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();
            var settings = configuration.LoadSettings<AppSettings>();

            return settings;
        }

        static AppSettings GetCurrentSettings()
        {
            FileInfo fi = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
            var location = Path.Combine(fi.DirectoryName);
            var builder = new ConfigurationBuilder()
                .SetBasePath(location)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            var configuration = builder.Build();
            var path = GetSettingsPath();
            var settings = GeneralSettingsReader.ReadGeneralSettingsLocal<AppSettings>(path);

            return settings;
        }

        static void SaveSettings(IReloadingManager<AppSettings> settings)
        {
            File.WriteAllText(GetSettingsPath(), JsonConvert.SerializeObject(settings.CurrentValue, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }

        static void SaveSettings(AppSettings settings)
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

//eth.sendTransaction({from:eth.coinbase, to:0xdd436bac8e76da5a9a73b1999c319dde2da85f8d, value: web3.toWei(0.05, "ether")}) 
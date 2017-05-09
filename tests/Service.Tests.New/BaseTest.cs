using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using AzureStorage.Queue;

namespace Tests
{
    public class BaseTest
    {
        public static string ColorCoin = "Lykke";
        public static string EthCoin = "Eth";


        public const string ClientA = "0x46Ea3e8d85A06cBBd8c6a491a09409f5B59BEa28";
        public const string PrivateKeyA = "0x1149984b590c0bcd88ca4e7ef80d2f4aa7b0bc0f52ac7895068e89262c8733c6";

        public const string ClientB = "0xb4d302df4f799a66702f8aa59543109f00573929";
        public const string PrivateKeyB = "e35e0dcaec4f5f2990cb9581d4531452b3eea9b7531bf6bf40eda95756799014";


        [TestInitialize]
        public async Task Up()
        {
            var config = new Config();
            await config.Initialize();
            //Config.Services.GetService<IUserContractRepository>().DeleteTable();
            Config.Services.GetService<IAppSettingsRepository>().DeleteTable();
            Config.Services.GetService<ICoinTransactionRepository>().DeleteTable();

            var queueFactory = Config.Services.GetService<Func<string, IQueueExt>>();

            queueFactory(Constants.ContractTransferQueue).ClearAsync().Wait();
            queueFactory(Constants.EthereumOutQueue).ClearAsync().Wait();
            queueFactory(Constants.CoinTransactionQueue).ClearAsync().Wait();
            queueFactory(Constants.TransactionMonitoringQueue).ClearAsync().Wait();
            queueFactory(Constants.CoinEventQueue).ClearAsync().Wait();

            Console.WriteLine("Setup test");
        }


        [TestCleanup]
        public void TearDown()
        {
            Console.WriteLine("Tear down");
        }

    }
}

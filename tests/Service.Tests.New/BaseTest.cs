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


        public const string ClientA = "0x9b6700a94c69c328473b6d517e2d121ee4ebe374";
        public const string PrivateKeyA = "7c7b1a1fcbab709113e8b3db6957e137d164fd4366fe30251ce4915f6675d73f";

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

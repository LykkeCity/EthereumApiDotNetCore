using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using AzureStorage.Queue;

namespace Tests
{
    public class BaseTest
    {
        public static string ColorCoin = "Lykke";
        public static string EthCoin = "Eth";

        protected static string _tokenAdapterAddress = "0x1e8e8ccbd9a7a8d82875054aa8342159d96356a9";//"0x27b1ad3f1ae08eec8205bcbe91166b6387d67c4f";
        protected static string _clientTokenTransferAddress = "0x7ff01d3225726eb3dd3356fc57e71e5ec0aab042";//"0x967ddcf62c2ecec1c4d231c7498c287b857846e7";
        protected static string _externalTokenAddress = "0xce2ef46ecc168226f33b6f6b8a56e90450d0d2c0";//"0x79e34063d05324e0bffc19901963d9ae5b101fba";
        protected static string _ethereumCoinOwnerB = "0xd513BeA430322c488600Af6eE094aB32238C7169";
        protected const string _clientA = "0x46Ea3e8d85A06cBBd8c6a491a09409f5B59BEa28";
        protected const string _privateKeyA = "0x1149984b590c0bcd88ca4e7ef80d2f4aa7b0bc0f52ac7895068e89262c8733c6";
        protected const string _clientB = "0xb4d302df4f799a66702f8aa59543109f00573929";
        protected const string _privateKeyB = "e35e0dcaec4f5f2990cb9581d4531452b3eea9b7531bf6bf40eda95756799014";


        [TestInitialize]
        public async Task Up()
        {
            var config = new Config();
            await config.Initialize();
            //Config.Services.GetService<IUserContractRepository>().DeleteTable();
            //Config.Services.GetService<IAppSettingsRepository>().DeleteTable();
            //Config.Services.GetService<ICoinTransactionRepository>().DeleteTable();

            var queueFactory = Config.Services.GetService<Func<string, IQueueExt>>();

            //queueFactory(Constants.ContractTransferQueue).ClearAsync().Wait();
            //queueFactory(Constants.EthereumOutQueue).ClearAsync().Wait();
            //queueFactory(Constants.CoinTransactionQueue).ClearAsync().Wait();
            //queueFactory(Constants.TransactionMonitoringQueue).ClearAsync().Wait();
            //queueFactory(Constants.CoinEventQueue).ClearAsync().Wait();

            Console.WriteLine("Setup test");
        }


        [TestCleanup]
        public void TearDown()
        {
            Console.WriteLine("Tear down");
        }

    }
}

using EthereumSamuraiApiCaller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Services.PrivateWallet;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using BusinessModels;
using System.Linq;
using System.Numerics;

namespace Service.UnitTests.PrivateWallet
{
    [TestClass]
    public class EthereumIndexerServiceUnitTest : BaseTest
    {
        private EthereumIndexerService _ethereumIndexerService;

        [TestInitialize]
        public void TestInit()
        {
            IEthereumSamuraiApi ethereumSamurai = Config.Services.GetService<IEthereumSamuraiApi>();
            _ethereumIndexerService = new EthereumIndexerService(ethereumSamurai);
        }

        [TestMethod]
        public async Task EthereumIndexerServiceUnitTest_TestQueryingTransactionsForAddress()
        {
            int count = 3;
            int start = 0;
            AddressTransactions addressTransactions = new AddressTransactions()
            {
                Address = TestConstants.PW_ADDRESS,
                Count = count,
                Start = start
            };
            IEnumerable<TransactionModel> transactions = await _ethereumIndexerService.GetTransactionHistory(addressTransactions);

            Assert.IsTrue(count - start >= transactions.Count());

            foreach (var transaction in transactions)
            {
                Assert.AreEqual(transaction.From, TestConstants.PW_ADDRESS);
            }
        }

        [TestMethod]
        public async Task EthereumIndexerServiceUnitTest_TestGetBalanceForAddress()
        {
            BigInteger balance = await _ethereumIndexerService.GetEthBalance(TestConstants.PW_ADDRESS);

            Assert.IsTrue(balance >= 0);
        }
    }
}

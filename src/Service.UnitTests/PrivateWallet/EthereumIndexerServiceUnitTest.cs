using EthereumSamuraiApiCaller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.BusinessModels;
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
            IEthereumSamuraiAPI ethereumSamurai = Config.Services.GetService<IEthereumSamuraiAPI>();
            _ethereumIndexerService = new EthereumIndexerService(ethereumSamurai);
        }

        [TestMethod]
        public async Task EthereumIndexerServiceUnitTest_TestQueryingTransactionsForAddress()
        {
            int count = 3;
            int start = 0;
            AddressTransaction addressTransactions = new AddressTransaction()
            {
                Address = TestConstants.PW_ADDRESS,
                Count = count,
                Start = start
            };
            IEnumerable<TransactionContentModel> transactions = await _ethereumIndexerService.GetTransactionHistory(addressTransactions);

            Assert.IsTrue(count - start >= transactions.Count());

            foreach (var transaction in transactions)
            {
                Assert.AreEqual(transaction.Transaction.From, TestConstants.PW_ADDRESS);
            }
        }

        [TestMethod]
        public async Task EthereumIndexerServiceUnitTest_TestQueryingInternalMessagesForAddress()
        {
            int count = 3;
            int start = 0;
            AddressTransaction addressTransactions = new AddressTransaction()
            {
                Address = TestConstants.PW_ADDRESS,
                Count = count,
                Start = start
            };
            IEnumerable<InternalMessageModel> messages = await _ethereumIndexerService.GetInternalMessagesHistory(addressTransactions);

            Assert.IsTrue(count - start >= messages.Count());

            foreach (var message in messages)
            {
                Assert.AreEqual(message.ToAddress, TestConstants.PW_ADDRESS);
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

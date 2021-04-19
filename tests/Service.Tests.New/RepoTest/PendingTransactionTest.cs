using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Tests;
using Lykke.Service.EthereumCore.Core.Repositories;
using System.Threading.Tasks;

namespace Service.Tests
{
    [TestClass]
    public class PendingTransactionTest : BaseTest
    {
        public IPendingTransactionsRepository _pendingTransactionsRepository { get; private set; }

        [TestInitialize]
        public void Init()
        {
            _pendingTransactionsRepository = Config.Services.GetService<IPendingTransactionsRepository>();
        }

        [TestMethod]
        public async Task PendingTransactionTest_TestIntegration()
        {
            await _pendingTransactionsRepository.InsertOrReplace(new PendingTransaction()
            {
                CoinAdapterAddress = "test",
                TransactionHash = "test",
                UserAddress = "test"
            });

            var transaction = await _pendingTransactionsRepository.GetAsync("test", "test");
            await _pendingTransactionsRepository.Delete(transaction.TransactionHash);
            transaction = await _pendingTransactionsRepository.GetAsync("test", "test");

            Assert.IsNull(transaction);
        }
    }
}

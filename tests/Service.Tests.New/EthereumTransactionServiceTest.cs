using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Tests;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Service.EthereumCore.Services;
using System.Threading.Tasks;

namespace Service.Tests
{
    [TestClass]
    public class EthereumTransactionServiceTest : BaseTest
    {
        private IEthereumTransactionService _transactionService;

        [TestInitialize]
        public void Init()
        {
            _transactionService = Config.Services.GetService<IEthereumTransactionService>();
        }

        [TestMethod]
        public async Task EthereumTransactionServiceTest_CheckTransactionIsPending()
        {
            string trHash = "0xa91534ba74bd42f9971fa2b54344bc043eb2e7b51e7017b90757ee042caaacc0";
            bool result = await _transactionService.IsTransactionInPool(trHash);

            Assert.IsFalse(result);
        }
    }
}

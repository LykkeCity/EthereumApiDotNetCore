using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Service.UnitTests
{
    public class BaseTest
    {
        [TestInitialize]
        public async Task Up()
        {
            var config = new Config();
            await config.Initialize();

            Console.WriteLine("Setup test");
        }


        [TestCleanup]
        public void TearDown()
        {
            Console.WriteLine("Tear down");
        }

    }
}

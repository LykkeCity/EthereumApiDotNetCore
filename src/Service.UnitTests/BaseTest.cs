using System;
using System.Threading.Tasks;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Common;
using Lykke.Service.EthereumCore.Core.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class BigDecimalTests
    {
        [TestMethod]
        public void TestDivide()
        {
            var decimalValue = 1234567;

            var value = new BigInteger(decimalValue);
            const decimal multy = 100M;

            var result = new BigDecimal(value, 0) / multy;
            Assert.AreEqual(decimalValue / multy, (decimal)result);
        }

        [TestMethod]
        public void TestMultuple()
        {
            decimal i = 33.4444M;
            var multy = new BigDecimal(10000, 0);
            var result = i * multy;
            Assert.AreEqual(new BigInteger(334444), new BigInteger((int)result));
        }

    }
}

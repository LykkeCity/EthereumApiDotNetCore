using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Service.EthereumCore.Services;
using System;
using Tests;
using Lykke.Service.EthereumCore.Core.Utils;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;
using Nethereum.Util;
using System.Linq;

namespace Service.Tests
{
    [TestClass]
    public class HashCalculationTest : BaseTest
    {
        public IHashCalculator _hashCalculator { get; private set; }

        [TestInitialize]
        public void Init()
        {
            _hashCalculator = Config.Services.GetService<IHashCalculator>();
        }

        [TestMethod]
        public void TestHash()
        {
            var guid = Guid.NewGuid();
            var amount = 100;
            var strForHash = EthUtils.GuidToByteArray(guid).ToHex() +
                            _tokenAdapterAddress.HexToByteArray().ToHex() +
                            _clientA.HexToByteArray().ToHex() +
                            _clientB.HexToByteArray().ToHex() +
                            EthUtils.BigIntToArrayWithPadding(new BigInteger(amount)).ToHex();

            byte[] expectedHash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());
            byte[] actualHash = _hashCalculator.GetHash(guid, _tokenAdapterAddress, _clientA, _clientB, amount);
            bool isEqual =Enumerable.SequenceEqual(expectedHash, actualHash);

            Assert.IsTrue(isEqual);
        }
    }
}

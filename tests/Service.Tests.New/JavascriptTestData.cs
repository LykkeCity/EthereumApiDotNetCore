using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Utils;
using Nethereum.ABI.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using Nethereum.Signer;
using Org.BouncyCastle.Math;

namespace Tests
{
    [TestClass]
    public class JavascriptTestData
    {
        const string account_a = "0x960336a077fB32d675405bd0A6cD0cb74aaa5062";
        const string account_b = "0xb295e245eD2fdf5776c3C8a49f0403BF0242262A";
        const string private_key_a = "4085dde01ea641a0f4fd6586ca11fc1f5df38e1bdcbef501da970cad9335b389";
        const string private_key_b = "74ed04f45c2a375a94189ef69661fa08235bb3b76be65934a0827262542e870c";
        const string color_coin_a = "0x5191790d72d511b401aaf866dfef221dc64f8695";
        const string color_coin_b = "0xebff73640ff3ca3e85f116033901d18f2764b380";

        [TestMethod]
        public void Data()
        {
            var guid = Guid.Parse("176a82d8-3154-4c76-bab8-441ad43d0de6");

            var strForHash = EthUtils.GuidToByteArray(guid).ToHex();

            var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());

            var sign = Sign(hash, private_key_a).ToHex();
        }

        [TestMethod]
        public void Test22()
        {
            var guid = new Guid("0b67caf2-0b02-4691-ac25-04858b0fa475");

            var strForHash = EthUtils.GuidToByteArray(guid).ToHex() +
                            "0x8c32ad594a6dc17b2e8b40af3df2c3ce1f79cdd4".HexToByteArray().ToHex() +
                            "0x5c711f0bfad342c4691b6e23cedcdf39e3fe03c1".HexToByteArray().ToHex() +
                            "0x33f53d968aee3cfe1ad460457a29827bfff33d8c".HexToByteArray().ToHex() +
                            EthUtils.BigIntToArrayWithPadding(9500).ToHex();

            var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());

            var sign = Sign(hash, "0x443dac9b6e682a887009cfd00690d0cac1905e494f84d03070174149c2d0cc76").ToHex();
            Assert.AreEqual("6ddfe17c1ff216df529277ff7fc94bff41b6984d3de36d2132f452986743de4c44831c1bd41d58043705a60aa853d6beeb8b2e0dcdb09805c5dd28b7e3e705ce1b", sign);
        }

        private byte[] Sign(byte[] hash, string privateKey)
        {
            var key = new EthECKey(privateKey.HexToByteArray(), true);
            var signature = key.SignAndCalculateV(hash);
            //ToByteArrayUnsigned
            var r = signature.R.ToHex();
            var s = signature.S.ToHex();
            var v = new[] { signature.V }.ToHex();

            var arr = (r + s + v).HexToByteArray();
            return arr;
        }
    }
}

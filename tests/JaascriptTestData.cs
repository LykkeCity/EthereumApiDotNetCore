using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Utils;
using NBitcoin.Crypto;
using Nethereum.ABI.Util;
using NUnit.Framework;
using Nethereum.Core.Signing.Crypto;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Tests
{
	[TestFixture]
	public class JaascriptTestData
	{
		const string account_a = "0x960336a077fB32d675405bd0A6cD0cb74aaa5062";
		const string account_b = "0xb295e245eD2fdf5776c3C8a49f0403BF0242262A";
		const string private_key_a = "4085dde01ea641a0f4fd6586ca11fc1f5df38e1bdcbef501da970cad9335b389";
		const string private_key_b = "74ed04f45c2a375a94189ef69661fa08235bb3b76be65934a0827262542e870c";
		const string color_coin_a = "0x5191790d72d511b401aaf866dfef221dc64f8695";
		const string color_coin_b = "0xebff73640ff3ca3e85f116033901d18f2764b380";

		[Test]
		public void Data()
		{
			var guid = Guid.Parse("176a82d8-3154-4c76-bab8-441ad43d0de6");

			var strForHash = EthUtils.GuidToByteArray(guid).ToHex();

			var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());

			var sign = Sign(hash, private_key_a).ToHex();
		}

		private byte[] Sign(byte[] hash, string privateKey)
		{
			var key = new ECKey(privateKey.HexToByteArray(), true);
			var signature = key.SignAndCalculateV(hash);

			var r = signature.R.ToByteArrayUnsigned().ToHex();
			var s = signature.S.ToByteArrayUnsigned().ToHex();
			var v = new[] { signature.V }.ToHex();

			var arr = (r + s + v).HexToByteArray();
			return arr;
		}
	}
}

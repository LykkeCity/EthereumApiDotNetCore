using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Core;
using Core.Settings;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin.Crypto;
using Nethereum.ABI.Encoders;
using Nethereum.ABI.Util;
using Services;
using Services.Coins;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Core.Signing.Crypto;
using Nethereum.Web3;
using Core.Utils;
using Nethereum.Hex.HexTypes;

namespace Tests
{
	[TestFixture]
	public class TestCoins : BaseTest
	{
		[Test]
		public async Task TestCashin()
		{
			var settings = Config.Services.GetService<IBaseSettings>();
			var colorCoin = settings.CoinContracts.FirstOrDefault(x => x.Value.Name == "Lykke");
			var coinService = Config.Services.GetService<ICoinContractService>();
			var cointTransactionService = Config.Services.GetService<ICoinTransactionService>();
			var transactionService = Config.Services.GetService<IEthereumTransactionService>();
			var currentBalance = await coinService.GetBalance(colorCoin.Key, ClientA);

			var amount = 100;

			var result = await coinService.CashIn(colorCoin.Key, ClientA, amount);

			while (await transactionService.GetTransactionReceipt(result) == null)
				await Task.Delay(100);

			Assert.IsTrue(await cointTransactionService.ProcessTransaction());

			var newBalance = await coinService.GetBalance(colorCoin.Key, ClientA);

			Assert.AreEqual(currentBalance + colorCoin.Value.GetInternalValue(amount), newBalance);
		}

		[Test]
		public async Task TestCashout()
		{
			var settings = Config.Services.GetService<IBaseSettings>();
			var colorCoin = settings.CoinContracts.FirstOrDefault(x => x.Value.Name == "Lykke");
			var coinService = Config.Services.GetService<ICoinContractService>();
			var transactionService = Config.Services.GetService<IEthereumTransactionService>();

			var currentBalance = await coinService.GetBalance(colorCoin.Key, ClientA);

			var amount = 100;

			var result = await coinService.CashIn(colorCoin.Key, ClientA, amount);

			while (await transactionService.GetTransactionReceipt(result) == null)
				await Task.Delay(100);

			var midBalance = await coinService.GetBalance(colorCoin.Key, ClientA);

			Assert.AreEqual(currentBalance + colorCoin.Value.GetInternalValue(amount), midBalance);

			var guid = Guid.NewGuid();

			var strForHash = EthUtils.GuidToByteArray(guid).ToHex() +
							colorCoin.Key.HexToByteArray().ToHex() +
							ClientA.HexToByteArray().ToHex() +
							ClientA.HexToByteArray().ToHex() +
							EthUtils.BigIntToArrayWithPadding(colorCoin.Value.GetInternalValue(amount)).ToHex();

			var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());

			var sign = Sign(hash, PrivateKeyA).ToHex();
			var cashout = await coinService.CashOut(guid, colorCoin.Key, ClientA, ClientA, amount, sign);

			while (await transactionService.GetTransactionReceipt(cashout) == null)
				await Task.Delay(100);

			Assert.IsTrue(await transactionService.IsTransactionExecuted(cashout, Constants.GasForCoinTransaction));

			var newBalance = await coinService.GetBalance(colorCoin.Key, ClientA);

			Assert.AreEqual(currentBalance, newBalance);
		}

		[Test]
		public async Task TestCoinSwap()
		{
			var settings = Config.Services.GetService<IBaseSettings>();
			var colorCoin = settings.CoinContracts.FirstOrDefault(x => x.Value.Name == "Lykke");
			var ethCoin = settings.CoinContracts.FirstOrDefault(x => x.Value.Name == "Eth");
			var coinService = Config.Services.GetService<ICoinContractService>();
			var transactionService = Config.Services.GetService<IEthereumTransactionService>();
			
			var currentBalance_a = await coinService.GetBalance(colorCoin.Key, ClientA);
			var currentBalance_b = await coinService.GetBalance(ethCoin.Key, ClientB);

			var amount_a = 100;
			var amount_b = 1;

			var cashin_a = await coinService.CashIn(colorCoin.Key, ClientA, amount_a);
			var cashin_b = await coinService.CashIn(ethCoin.Key, ClientB, amount_b, true);

			while (await transactionService.GetTransactionReceipt(cashin_a) == null)
				await Task.Delay(100);

			while (await transactionService.GetTransactionReceipt(cashin_b) == null)
				await Task.Delay(100);

			var midBalance_a = await coinService.GetBalance(colorCoin.Key, ClientA);
			var midBalance_b = await coinService.GetBalance(ethCoin.Key, ClientB);

			Assert.AreEqual(currentBalance_a + colorCoin.Value.GetInternalValue(amount_a), midBalance_a);
			Assert.AreEqual(currentBalance_b + ethCoin.Value.GetInternalValue(amount_b), midBalance_b);

			var swap_amount_a = 50;
			var swap_amount_b = 0.1m;

			var guid = Guid.NewGuid();

			var strForHash = EthUtils.GuidToByteArray(guid).ToHex() +
							ClientA.HexToByteArray().ToHex() +
							ClientB.HexToByteArray().ToHex() +
							colorCoin.Key.HexToByteArray().ToHex() +
							ethCoin.Key.HexToByteArray().ToHex() +
							EthUtils.BigIntToArrayWithPadding(colorCoin.Value.GetInternalValue(swap_amount_a)).ToHex() +
							EthUtils.BigIntToArrayWithPadding(ethCoin.Value.GetInternalValue(swap_amount_b)).ToHex();

			var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());

			var sign_a = Sign(hash, PrivateKeyA).ToHex();
			var sign_b = Sign(hash, PrivateKeyB).ToHex();

			var swap = await coinService.Swap(guid, ClientA, ClientB, colorCoin.Key, ethCoin.Key, swap_amount_a, swap_amount_b, sign_a, sign_b);

			while (await transactionService.GetTransactionReceipt(swap) == null)
				await Task.Delay(100);

			Assert.IsTrue(await transactionService.IsTransactionExecuted(swap, Constants.GasForCoinTransaction));

			var newBalance_a = await coinService.GetBalance(colorCoin.Key, ClientA);
			var newBalance_b = await coinService.GetBalance(ethCoin.Key, ClientB);

			Assert.AreEqual(midBalance_a - colorCoin.Value.GetInternalValue(swap_amount_a), newBalance_a);
			Assert.AreEqual(midBalance_b - ethCoin.Value.GetInternalValue(swap_amount_b), newBalance_b);
		}

		private byte[] Sign(byte[] hash, string privateKey)
		{
			var key = new ECKey(privateKey.HexToByteArray(), true);
			var signature = key.SignAndCalculateV(hash);

			var r = signature.R.ToByteArrayUnsigned().ToHex();
			var s = signature.S.ToByteArrayUnsigned().ToHex();
			var v = new[] { signature.V }.ToHex();

			var arr = (r + s + v).HexToByteArray();
			return FixBytesOrder(arr);
		}

		private byte[] FixBytesOrder(byte[] source)
		{
			if (!BitConverter.IsLittleEndian)
				return source;

			return source.Reverse().ToArray();
		}
	}
}

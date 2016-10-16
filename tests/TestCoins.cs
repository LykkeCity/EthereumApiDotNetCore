using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
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
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;
using Services.Coins.Models;

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

			var result = await coinService.CashIn(Guid.NewGuid(), colorCoin.Key, ClientA, amount);

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

			var result = await coinService.CashIn(Guid.NewGuid(), colorCoin.Key, ClientA, amount);

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
			var amount_b = 0.01M;

			var cashin_a = await coinService.CashIn(Guid.NewGuid(), colorCoin.Key, ClientA, amount_a);
			var cashin_b = await coinService.CashIn(Guid.NewGuid(), ethCoin.Key, ClientB, amount_b);

			while (await transactionService.GetTransactionReceipt(cashin_a) == null)
				await Task.Delay(100);

			while (await transactionService.GetTransactionReceipt(cashin_b) == null)
				await Task.Delay(100);

			var midBalance_a = await coinService.GetBalance(colorCoin.Key, ClientA);
			var midBalance_b = await coinService.GetBalance(ethCoin.Key, ClientB);

			Assert.AreEqual(currentBalance_a + colorCoin.Value.GetInternalValue(amount_a), midBalance_a);
			Assert.AreEqual(currentBalance_b + ethCoin.Value.GetInternalValue(amount_b), midBalance_b);

			var swap_amount_a = 50;
			var swap_amount_b = 0.01m;

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


		[Test]
		public async Task TestCoinEvents()
		{
			var settings = Config.Services.GetService<IBaseSettings>();
			var colorCoin = settings.CoinContracts.FirstOrDefault(x => x.Value.Name == "Lykke");
			var ethCoin = settings.CoinContracts.FirstOrDefault(x => x.Value.Name == "Eth");
			var coinService = Config.Services.GetService<ICoinContractService>();
			var transactionService = Config.Services.GetService<IEthereumTransactionService>();
			var queueFactory = Config.Services.GetService<Func<string, IQueueExt>>();
			var eventQueue = queueFactory(Constants.CoinEventQueue);

			await coinService.GetCoinContractFilters(true);
			decimal amountColor = 2, amountEth = 0.02M, amountColorOut = 1, amountEthOut = 0.01M, amountColorSwap = 0.5M, amountEthSwap = 0.005M;

			var cashin1 = await coinService.CashIn(Guid.NewGuid(), colorCoin.Key, ClientA, amountColor);
			var cashin2 = await coinService.CashIn(Guid.NewGuid(), ethCoin.Key, ClientB, amountEth);

			var guid1 = Guid.NewGuid();
			var guid2 = Guid.NewGuid();

			var strForHash = EthUtils.GuidToByteArray(guid1).ToHex() +
						colorCoin.Key.HexToByteArray().ToHex() +
						ClientA.HexToByteArray().ToHex() +
						ClientB.HexToByteArray().ToHex() +
						EthUtils.BigIntToArrayWithPadding(colorCoin.Value.GetInternalValue(amountColorOut)).ToHex();
			var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());


			var strForHash2 = EthUtils.GuidToByteArray(guid2).ToHex() +
					ethCoin.Key.HexToByteArray().ToHex() +
					ClientB.HexToByteArray().ToHex() +
					ClientA.HexToByteArray().ToHex() +
					EthUtils.BigIntToArrayWithPadding(ethCoin.Value.GetInternalValue(amountEthOut)).ToHex();
			var hash2 = new Sha3Keccack().CalculateHash(strForHash2.HexToByteArray());

			var cashOut1 = await coinService.CashOut(guid1, colorCoin.Key, ClientA, ClientB, amountColorOut, Sign(hash, PrivateKeyA).ToHex());
			var cashOut2 = await coinService.CashOut(guid2, ethCoin.Key, ClientB, ClientA, amountEthOut, Sign(hash2, PrivateKeyB).ToHex());

			var swapGuid = Guid.NewGuid();

			var strForHash3 = EthUtils.GuidToByteArray(swapGuid).ToHex() +
							ClientA.HexToByteArray().ToHex() +
							ClientB.HexToByteArray().ToHex() +
							colorCoin.Key.HexToByteArray().ToHex() +
							ethCoin.Key.HexToByteArray().ToHex() +
							EthUtils.BigIntToArrayWithPadding(colorCoin.Value.GetInternalValue(amountColorSwap)).ToHex() +
							EthUtils.BigIntToArrayWithPadding(ethCoin.Value.GetInternalValue(amountEthSwap)).ToHex();
			var hash3 = new Sha3Keccack().CalculateHash(strForHash3.HexToByteArray());

			var swap3 = await coinService.Swap(swapGuid, ClientA, ClientB, colorCoin.Key, ethCoin.Key, amountColorSwap, amountEthSwap,
				Sign(hash3, PrivateKeyA).ToHex(), Sign(hash3, PrivateKeyB).ToHex());


			var transactions = new List<string> { cashin1, cashin2, cashOut1, cashOut2, swap3 };


			while (transactions.Count > 0)
			{
				foreach (var trHash in transactions.ToList())
				{
					if ((await transactionService.GetTransactionReceipt(trHash)) != null)
						transactions.Remove(trHash);
				}
				await Task.Delay(100);
			}

			await coinService.RetrieveEventLogs(false);

			var messages = new List<CoinContractPublicEvent>();
			for (int i = 0; i < 6; i++)
			{
				var msg = await eventQueue.GetRawMessageAsync();
				if (msg != null)
					messages.Add(JsonConvert.DeserializeObject<CoinContractPublicEvent>(msg.AsString));
			}

			Assert.IsTrue(messages.Any(o => o.EventName == Constants.CashInEvent && o.Address == colorCoin.Key && o.Amount == amountColor && o.Caller == ClientA),
				"not found cashin1");
			Assert.IsTrue(messages.Any(o => o.EventName == Constants.CashInEvent && o.Address == ethCoin.Key && o.Amount == amountEth && o.Caller == ClientB),
				"not found cashin2");
			Assert.IsTrue(messages.Any(o => o.EventName == Constants.CashOutEvent && o.Address == colorCoin.Key && o.Amount == amountColorOut && o.From == ClientA),
				"not found cashout1");
			Assert.IsTrue(messages.Any(o => o.EventName == Constants.CashOutEvent && o.Address == ethCoin.Key && o.Amount == amountEthOut && o.From == ClientB),
				"not found cashout2");
			Assert.IsTrue(messages.Any(o => o.EventName == Constants.TransferEvent && o.Address == colorCoin.Key && o.Amount == amountColorSwap && o.From == ClientA && o.To == ClientB),
				"not found swap1");
			Assert.IsTrue(messages.Any(o => o.EventName == Constants.TransferEvent && o.Address == ethCoin.Key && o.Amount == amountEthSwap && o.From == ClientB && o.To == ClientA),
				"not found swap2");
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

		private byte[] FixBytesOrder(byte[] source)
		{
			if (!BitConverter.IsLittleEndian)
				return source;

			return source.Reverse().ToArray();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Web3;
using Services;
using System.Diagnostics;
using Core;
using Core.Settings;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;

namespace Tests
{
	[TestFixture]
	public class TestContracts : BaseTest
	{
		[Test]
		public async Task TestUserContractBigAmount()
		{
			var address = "0xeac3466d109a22e8c51e066652e09dc85ec33615";
			var settings = Config.Services.GetService<IBaseSettings>();
			var ethereumtransactionService = Config.Services.GetService<IEthereumTransactionService>();

			var web3 = new Web3(settings.EthereumUrl);

			// unlock account for 120 seconds
			await web3.Personal.UnlockAccount.SendRequestAsync(settings.EthereumMainAccount, settings.EthereumMainAccountPassword, new HexBigInteger(120));

			var contract = web3.Eth.GetContract(settings.UserContract.Abi, address);

			var function = contract.GetFunction("transferMoney");

			var transaction = await function.SendTransactionAsync(settings.EthereumMainAccount, new HexBigInteger(Constants.GasForUserContractTransafer), new HexBigInteger(0), settings.EthereumPrivateAccount, 100m);

			while (await ethereumtransactionService.GetTransactionReceipt(transaction) == null)
				await Task.Delay(100);

			Assert.IsFalse(await ethereumtransactionService.IsTransactionExecuted(transaction, Constants.GasForUserContractTransafer));
		}

		[Test]
		public async Task TestUserContractWrongSender()
		{
			var account = "0x5912216a589cDEBc95798f2709c2D5a88c562bdB";
			var pasword = "123456";
			var address = "0xeac3466d109a22e8c51e066652e09dc85ec33615";
			var settings = Config.Services.GetService<IBaseSettings>();
			var ethereumtransactionService = Config.Services.GetService<IEthereumTransactionService>();

			var web3 = new Web3(settings.EthereumUrl);

			// unlock account for 120 seconds
			await web3.Personal.UnlockAccount.SendRequestAsync(account, pasword, new HexBigInteger(120));

			var contract = web3.Eth.GetContract(settings.UserContract.Abi, address);

			var function = contract.GetFunction("transferMoney");

			var transaction = await function.SendTransactionAsync(settings.EthereumMainAccount, new HexBigInteger(Constants.GasForUserContractTransafer), new HexBigInteger(0), settings.EthereumPrivateAccount, 1m);

			while (await ethereumtransactionService.GetTransactionReceipt(transaction) == null)
				await Task.Delay(100);

			Assert.IsTrue(await ethereumtransactionService.IsTransactionExecuted(transaction, Constants.GasForUserContractTransafer));
		}

		public class TansactionTrace
		{
			public int Gas { get; set; }
			public string ReturnValue { get; set; }
			public TransactionStructLog[] StructLogs { get; set; }
		}

		public class TransactionStructLog
		{
			public string Error { get; set; }
		}

	}
}

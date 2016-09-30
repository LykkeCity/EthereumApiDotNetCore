using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Web3;
using Services;
using System.Diagnostics;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Settings;
using EthereumJobs.Job;
using Microsoft.WindowsAzure.Storage.Queue;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;

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

		[Test]
		public async Task TestRefillAccount()
		{
			var amount = 0.01M;

			var contractService = Config.Services.GetService<IContractService>();
			var settings = Config.Services.GetService<IBaseSettings>();
			var ethereumtransactionService = Config.Services.GetService<IEthereumTransactionService>();
			var queueFactory = Config.Services.GetService<Func<string, IQueueExt>>();
			var firePaymentEventsQueue = queueFactory(Constants.EthereumOutQueue);
			var transferContractQueue = queueFactory(Constants.ContractTransferQueue);


			await contractService.CreateFilterEventForUserContractPayment();
			var contract = await contractService.GenerateUserContract();

			var web3 = new Web3(settings.EthereumUrl);
			await web3.Personal.UnlockAccount.SendRequestAsync(settings.EthereumMainAccount, settings.EthereumMainAccountPassword, new HexBigInteger(120));
			var tr = await web3.Eth.Transactions.SendTransaction.SendRequestAsync(new TransactionInput(null, contract, settings.EthereumMainAccount, new HexBigInteger(Constants.GasForUserContractTransafer), new HexBigInteger(UnitConversion.Convert.ToWei(amount))));

			while (await ethereumtransactionService.GetTransactionReceipt(tr) == null)
				await Task.Delay(100);

			var checkPaymentJob = Config.Services.GetService<CheckPaymentsToUserContractsJob>();
			await checkPaymentJob.Execute();

			var transferTransactionJob = Config.Services.GetService<TransferTransactionQueueJob>();

			var transferTr = JsonConvert.DeserializeObject<ContractTransferTransaction>(
					(await transferContractQueue.PeekRawMessageAsync()).AsString).TransactionHash;

			while (await ethereumtransactionService.GetTransactionReceipt(transferTr) == null)
				await Task.Delay(100);

			CloudQueueMessage paymentEvent = null;
			int maxTryCnt = 10;

			while ((paymentEvent = await firePaymentEventsQueue.GetRawMessageAsync()) == null && maxTryCnt-- > 0)
			{
				await transferTransactionJob.Execute();
				await Task.Delay(300);
			}
			Assert.NotNull(paymentEvent);

			var evnt = JsonConvert.DeserializeObject<EthereumCashInModel>(paymentEvent.AsString);

			Assert.AreEqual(contract, evnt.Contract);
			Assert.AreEqual(amount, evnt.Amount);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using EthereumJobs.Job;
using NUnit.Framework;
using Tests;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Services;
using AzureStorage.Queue;

namespace Tests.JobTests
{
	[TestFixture]
	public class TransferTransactionQueueJobTests : BaseTest
	{
		[Test]
		public async Task TestTransferTransactionExecute()
		{

			var amount = 1.21M;
			var tr = "0x19e54753324312ed1bcb9c1e57599b702c220ca5b5e93fabc5fb1466240caf7e";
			var contract = "0x827F6785D9Ab8A308bc3b906789762fB87fF03b7";

			var queueFactory = Config.Services.GetService<Func<string, IQueueExt>>();

			var contractTransferQueue = queueFactory(Constants.ContractTransferQueue);
			await contractTransferQueue.PutRawMessageAsync(JsonConvert.SerializeObject(new ContractTransferTransaction()
			{
				Contract = contract,
				TransactionHash = tr,
				Amount = amount
			}));

			var job = Config.Services.GetService<TransferTransactionQueueJob>();
			await job.Execute();

			var firePaymentEventsQueueu = queueFactory(Constants.EthereumOutQueue);
			var evnt = JsonConvert.DeserializeObject<EthereumCashInModel>((await firePaymentEventsQueueu.GetRawMessageAsync()).AsString);

			Assert.AreEqual(amount, evnt.Amount);
			Assert.AreEqual(tr, evnt.TransactionHash);
			Assert.AreEqual(contract, evnt.Contract);
		}

	}
}

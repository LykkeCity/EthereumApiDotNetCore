using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using NUnit.Framework.Internal;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Services;
using AzureStorage.Queue;

namespace Tests
{
	[TestFixture]
	internal class EthereumQueueOutTests : BaseTest
	{
		[Test]
		public async Task TestFirePaymentEvent()
		{
			var contract = "testcontract";
			decimal amount = 5;
			var trHash = "12345";

			var service = Config.Services.GetService<IEthereumQueueOutService>();
			var query = Config.Services.GetService<Func<string, IQueueExt>>()(Constants.EthereumOutQueue);

			await service.FirePaymentEvent(contract, amount, trHash);
			var msg = await query.GetRawMessageAsync();
			var evnt = JsonConvert.DeserializeObject<EthereumCashInModel>(msg.AsString);
			Assert.AreEqual(contract, evnt.Contract);
			Assert.AreEqual(amount, evnt.Amount);
			Assert.AreEqual(trHash, evnt.TransactionHash);
			await query.FinishRawMessageAsync(msg);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Repositories;
using NUnit.Framework.Internal;
using NUnit.Framework;
using Services;
using Microsoft.Extensions.DependencyInjection;

namespace Tests
{
	[TestFixture]
	internal class ContractQueueServiceTests : BaseTest
	{
		[Test]
		public async Task TestPushAndGetContract()
		{
			var contractQueueService = Config.Services.GetService<IContractQueueService>();

			var contract = "testcontract";
			await contractQueueService.PushContract(contract);
			Assert.AreEqual(contract, await contractQueueService.GetContract());

			Assert.ThrowsAsync<BackendException>(() => contractQueueService.GetContract());
		}

	    [Test]
		public async Task TestSaveUserContract()
		{
			var contractQueueService = Config.Services.GetService<IContractQueueService>();
			var userContractRepositoty = Config.Services.GetService<IUserContractRepository>();

			var contract = "testcontract";
			await contractQueueService.PushContract(contract);

			await contractQueueService.GetContractAndSave();

			var userContract = await userContractRepositoty.GetUserContractAsync(contract);

			Assert.NotNull(userContract);
			Assert.AreEqual(contract, userContract.Address);

		}
	}
}

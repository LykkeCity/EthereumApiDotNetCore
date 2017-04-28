using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.ContractEvents;
using Core.Repositories;
using EthereumJobs.Job;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Services;
using Common.Log;

namespace Tests.JobTests
{
	[TestFixture]
	public class TestMonitoringContractBalanceJob : BaseTest
	{
		[Test]
		public async Task TestMonitoringContractBalanceJobExecute()
		{
			var events = new List<UserPaymentEvent>();

			var mock = new Mock<IPaymentService>();
			mock.Setup(s => s.GetUserContractBalance(It.IsAny<string>()))
				.Returns(() => Task.FromResult(1.0M));

			mock.Setup(s => s.ProcessPaymentEvent(It.IsAny<UserPaymentEvent>()))
				.Returns<UserPaymentEvent>(@event =>
				{
					events.Add(@event);
					return Task.FromResult(true);
				});

			var userContractRepo = Config.Services.GetService<IUserContractRepository>();
			await userContractRepo.AddAsync(new UserContract { Address = "test", CreateDt = DateTime.UtcNow });

			var job = new MonitoringContractBalance(Config.Services.GetService<IUserContractRepository>(),
				Config.Services.GetService<ILog>(), mock.Object, Config.Services.GetService<IEmailNotifierService>());
			for (var i = 0; i < 5; i++)
				await job.Execute();
			Assert.AreEqual(1, events.Count);
			Assert.AreEqual("test", events[0].Address);
		}

	}
}

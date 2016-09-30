using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Repositories;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace Tests
{
    public class BaseTest
    {
		[SetUp]
	    public void Up()
	    {
			Config.Services.GetService<IUserContractRepository>().DeleteTable();
			Config.Services.GetService<IAppSettingsRepository>().DeleteTable();

			var queueFactory = Config.Services.GetService<Func<string, IQueueExt>>();

			queueFactory(Constants.ContractTransferQueue).ClearAsync().Wait();
			queueFactory(Constants.EthereumOutQueue).ClearAsync().Wait();

			Console.WriteLine("Setup test");
	    }


		[TearDown]
	    public void TearDown()
		{			
			Console.WriteLine("Tear down");
		}

    }
}

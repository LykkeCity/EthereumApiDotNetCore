using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
		    Console.WriteLine("Setup test");
	    }


		[TearDown]
	    public void TearDown()
		{
			var userContractsRepo = Config.Services.GetService<IUserContractRepository>();
			userContractsRepo.DeleteTable();
			Console.WriteLine("Tear down");
		}

    }
}

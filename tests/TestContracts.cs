using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Services;

namespace Tests
{
	[TestFixture]
    public class TestContracts : BaseTest
    {
		[Test]
		public void TestContract()
		{
			var paymentService = Config.Services.GetService<IPaymentService>();
			Assert.NotNull(paymentService);
			//paymentService.TransferFromUserContract("")

		}

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Web3;
using Services;
using System.Diagnostics;

namespace Tests
{
	[TestFixture]
	public class TestContracts : BaseTest
	{
		//[Test]
		public async Task TestContract()
		{
			var contractService = Config.Services.GetService<IContractService>();
			//var paymentService = Config.Services.GetService<IPaymentService>();
			//Assert.NotNull(paymentService);
			//var tr = await paymentService.TransferFromUserContract("0x827f6785d9ab8a308bc3b906789762fb87ff03b7", UnitConversion.Convert.ToWei(1));
			//Debug.WriteLine(tr);
			var tr = "0x545ac4240e9b14e3a15de2bac0898aafdf45df088451b5b4adade6d0173f6fd1";
			var r = await contractService.GetTransactionReceipt(tr);
			var web3 = new Web3("http://localhost:8000/");
			var logs =
			 await
			  web3.DebugGeth.TraceTransaction.SendRequestAsync(tr,
			   new Nethereum.RPC.DebugGeth.DTOs.TraceTransactionOptions());

			var obj = logs.ToObject<TansactionTrace>();
			if (obj.StructLogs?.Length > 0 && !string.IsNullOrWhiteSpace(obj.StructLogs[obj.StructLogs.Length - 1].Error))
			{
				var str = obj.StructLogs[obj.StructLogs.Length - 1].Error;
			}
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

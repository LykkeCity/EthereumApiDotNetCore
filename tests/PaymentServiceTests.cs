using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.RPC.Eth.DTOs;
using Services;

namespace Tests
{
	[TestFixture]
	internal class PaymentServiceTests : BaseTest
	{
		[Test]
		public async Task TestGetMainBalance()
		{
			var service = Config.Services.GetService<IPaymentService>();
			var balance = await service.GetMainAccountBalance();
			Assert.Greater(balance, 0);
		}

		[Test]
		public async Task TestTransferFromUserContract()
		{
			var contract = "0x827F6785D9Ab8A308bc3b906789762fB87fF03b7";
			var balance = 1.1M;
			var service = Config.Services.GetService<IPaymentService>();
			var contractService = Config.Services.GetService<IContractService>();

			var exep = Assert.ThrowsAsync<Exception>(async () => await service.TransferFromUserContract(contract, balance));
			Assert.IsTrue(exep.Message.Contains("TransferFromUserContract failed, contract balance is"));
			balance = 0;

			var tr = await service.TransferFromUserContract(contract, balance);
			Assert.NotNull(tr);

			TransactionReceipt receipt = null;
			while ((receipt = await contractService.GetTransactionReceipt(tr)) == null)
			{
				await Task.Delay(100);
			}
			Assert.Greater((int)receipt.GasUsed.Value, 0);
		}


	}
}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Numerics;
//using System.Threading.Tasks;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Microsoft.Extensions.DependencyInjection;
//using Nethereum.RPC.Eth.DTOs;
//using Services;

//namespace Tests
//{
//    [TestClass]
//    internal class PaymentServiceTests : BaseTest
//    {
//        [TestMethod]
//        public async Task TestGetMainBalance()
//        {
//            var service = Config.Services.GetService<IPaymentService>();
//            var balance = await service.GetMainAccountBalance();
//            Assert.IsTrue(balance > 0);
//        }

//        [TestMethod]
//        public async Task TestTransferFromUserContract()
//        {
//            var contract = "0x827F6785D9Ab8A308bc3b906789762fB87fF03b7";
//            var balance = 1.1M;
//            var service = Config.Services.GetService<IPaymentService>();
//            var ethereumtransactionService = Config.Services.GetService<IEthereumTransactionService>();

//            var exep = await Assert.ThrowsExceptionAsync<Exception>(async () => await service.TransferFromUserContract(contract, balance));
//            Assert.IsTrue(exep.Message.Contains("TransferFromUserContract failed, contract balance is"));
//            balance = 0;

//            var tr = await service.TransferFromUserContract(contract, balance);
//            Assert.IsNotNull(tr);

//            TransactionReceipt receipt = null;
//            while ((receipt = await ethereumtransactionService.GetTransactionReceipt(tr)) == null)
//            {
//                await Task.Delay(100);
//            }
//            Assert.IsTrue((int)receipt.GasUsed.Value > 0);
//        }


//    }
//}

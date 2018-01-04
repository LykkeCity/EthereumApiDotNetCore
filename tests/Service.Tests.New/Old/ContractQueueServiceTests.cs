//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Lykke.Service.EthereumCore.Core.Exceptions;
//using Lykke.Service.EthereumCore.Core.Repositories;
//using NUnit.Framework.Internal;
//using NUnit.Framework;
//using Lykke.Service.EthereumCore.Services;
//using Microsoft.Extensions.DependencyInjection;

//namespace Tests
//{
//    [TestFixture]
//    internal class ContractQueueServiceTests : BaseTest
//    {
//        [Test]
//        public async Task TestPushAndGetContract()
//        {
//            var contractQueueService = Config.Services.GetService<IContractQueueService>();

//            var contract = "testcontract";
//            await contractQueueService.PushContract(contract);
//            Assert.AreEqual(contract, await contractQueueService.GetContract());

//            Assert.ThrowsAsync<BackendException>(() => contractQueueService.GetContract());
//        }

//        [Test]
//        public async Task TestSaveUserContract()
//        {
//            var contractQueueService = Config.Services.GetService<IContractQueueService>();
//            var userContractRepositoty = Config.Services.GetService<IUserContractRepository>();

//            var contract = "testcontract";
//            var wallet = "user-wallet";
//            await contractQueueService.PushContract(contract);

//            await contractQueueService.GetContractAndSave(wallet);

//            var userContract = await userContractRepositoty.GetUserContractAsync(contract);

//            Assert.NotNull(userContract);
//            Assert.AreEqual(contract, userContract.Address);
//            Assert.AreEqual(wallet, userContract.UserWallet);

//        }
//    }
//}

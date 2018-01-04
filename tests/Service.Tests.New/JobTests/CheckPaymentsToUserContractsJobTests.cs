//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Lykke.Service.EthereumCore.Core;
//using Lykke.Service.EthereumCore.Core.ContractEvents;
//using Lykke.Service.EthereumCore.Core.Repositories;
//using Lykke.Job.EthereumCore.Job;
//using Moq;
//using NUnit.Framework;
//using Lykke.Service.EthereumCore.Services;
//using Microsoft.Extensions.DependencyInjection;

//namespace Tests.JobTests
//{
//    [TestFixture]
//    public class CheckPaymentsToUserContractsJobTests : BaseTest
//    {
//        [Test]
//        public async Task TestCheckPaymentsToUserContractsJobExecute()
//        {

//            var events = new List<UserPaymentEvent>();

//            var mock = new Mock<IPaymentService>();

//            mock.Setup(s => s.ProcessPaymentEvent(It.IsAny<UserPaymentEvent>()))
//                .Returns<UserPaymentEvent>(@event =>
//                {
//                    events.Add(@event);
//                    return Task.FromResult(true);
//                });

//            var job = new CheckPaymentsToUserContractsJob(Config.Services.GetService<IContractService>(), mock.Object,
//                Config.Logger);
//            var appSettingsRepo = Config.Services.GetService<IAppSettingsRepository>();

//            await job.Execute();

//            Assert.AreEqual(0, events.Count);
//            var filter = await appSettingsRepo.GetSettingAsync(Constants.EthereumFilterSettingKey);
//            Assert.NotNull(filter);
//        }

//    }
//}

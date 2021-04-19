using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using Tests;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Service.EthereumCore.Core.Repositories;

namespace Service.Tests.BugReproduction
{
    [TestClass]
    public class EventTraceTest : BaseTest
    {
        public IEventTraceRepository _eventTraceRepository { get; private set; }

        [TestInitialize]
        public void Init()
        {
            _eventTraceRepository = Config.Services.GetService<IEventTraceRepository>();
        }

        [TestMethod]
        public async Task Get_GasPriceTest()
        {
            for (int i = 0; i < 1000; i++)
            {
                await _eventTraceRepository.InsertAsync(new EventTrace()
                {
                    Note = "Test",
                    OperationId = "Test",
                    TraceDate = DateTime.UtcNow
                });
            }
        }
    }
}

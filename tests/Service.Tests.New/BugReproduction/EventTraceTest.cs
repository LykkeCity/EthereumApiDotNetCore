using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tests;
using Microsoft.Extensions.DependencyInjection;
using SigningServiceApiCaller;
using Lykke.Service.EthereumCore.Services;
using System.Numerics;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Diagnostics;
using LkeServices.Signature;
using Nethereum.Web3;
using Nethereum.RPC.Eth.DTOs;
using Lykke.Service.EthereumCore.Core.Settings;
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

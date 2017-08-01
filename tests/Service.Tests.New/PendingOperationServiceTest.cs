using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Core;
using Core.Repositories;
using Core.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.ABI.Encoders;
using Nethereum.ABI.Util;
using Services;
using Services.Coins;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Core.Utils;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;
using Services.Coins.Models;
using AzureStorage.Queue;
using Nethereum.Util;
using Nethereum.Signer;
using SigningServiceApiCaller;
using Common.Log;
using Core.Notifiers;
using Moq;

namespace Tests
{
    //Todo: put tests on separate tables
    //Warning: tests consumes ethereum on mainAccount. Run on testnet only!
    [TestClass]
    public class PendingOperationServiceTest : BaseTest
    {
        public PendingOperationService _pendingOperationService { get; private set; }

        [TestInitialize]
        public void Init()
        {
            var settings = Config.Services.GetService<IBaseSettings>();
            var operationToHashMatchRepo = new Moq.Mock<IOperationToHashMatchRepository>();
            var pendingOperationRepo = new Moq.Mock<IPendingOperationRepository>();
            var queueFactory = new Moq.Mock<IQueueFactory>();
            var web3 = Config.Services.GetService<Web3>();
            var hashCalculator = Config.Services.GetService<IHashCalculator>();
            var coinRepository = new Moq.Mock<ICoinRepository>();
            var signingApi = Config.Services.GetService<ILykkeSigningAPI>();
            var log = new Moq.Mock<ILog>();
            var slack = new Moq.Mock<ISlackNotifier>();
            var queueMock = new Moq.Mock<IQueueExt>();
            var eventTraceMock = new Moq.Mock<IEventTraceRepository>();
            queueFactory.Setup(x => x.Build(It.IsAny<string>())).Returns(queueMock.Object);

            _pendingOperationService = new PendingOperationService(settings,
                operationToHashMatchRepo.Object,
                pendingOperationRepo.Object,
                queueFactory.Object,
                web3,
                hashCalculator,
                coinRepository.Object,
                signingApi,
                log.Object, 
                slack.Object,
                eventTraceMock.Object
                );
        }

        [TestMethod]
        public async Task PendingOperationServiceTest_TestTransfer()
        {
            var id = Guid.NewGuid();
            var amount = new BigInteger(100);
            var from = _clientA;
            var to = _clientA;

            await _pendingOperationService.Transfer(id, _clientTokenTransferAddress, from, to, amount,"");
        }
    }
}

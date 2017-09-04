using Common.Log;
using Core;
using Core.Repositories;
using Core.Settings;
using EthereumJobs.Job;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Services;
using Services.Coins;
using Services.New.Models;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Service.UnitTests.Jobs
{
    /// <summary>
    /// 1. Skip processing if there is no balance
    /// 2. Process 
    /// </summary>
    [TestClass]
    public class MonitoringOperationJobUnitTest : BaseTest
    {
        private Mock<ILog> _log;
        private Mock<IBaseSettings> _baseSettings;
        private Mock<IPendingOperationService> _pendingOperationService;
        private Mock<IExchangeContractService> _exchangeContractService;
        private Mock<ICoinEventService> _coinEventService;
        private Mock<ITransferContractService> _transferContractService;
        private Mock<IEventTraceRepository> _eventTraceRepository;
        public Mock<IQueueFactory> _queueFactory;

        [TestInitialize]
        public void Init()
        {
            _log = new Mock<ILog>();
            _baseSettings = new Mock<IBaseSettings>();
            _pendingOperationService = new Mock<IPendingOperationService>();
            _exchangeContractService = new Mock<IExchangeContractService>();
            _coinEventService = new Mock<ICoinEventService>();
            _transferContractService = new Mock<ITransferContractService>();
            _eventTraceRepository = new Mock<IEventTraceRepository>();
            _queueFactory = new Mock<IQueueFactory>();
        }

        //1.
        [TestMethod]
        public async Task MonitoringOperationJobUnitTest_SkipIfNoBalance()
        {
            string operationId = Guid.NewGuid().ToString();
            OperationHashMatchMessage message = new OperationHashMatchMessage()
            {
                OperationId = operationId,
                PutDateTime = DateTime.UtcNow,
                TransactionHash = null
            };

            #region Arrange Mocks
            IPendingOperation pendingOperation = new PendingOperation()
            {
                Amount = "1000000000000000000",
                FromAddress = "fromAddress",
                OperationId = operationId,
                OperationType = OperationTypes.Transfer,
                ToAddress = "toAddress",
                CoinAdapterAddress = "coinAdapter",
            };

            _pendingOperationService.Setup(x => x.GetOperationAsync(pendingOperation.OperationId)).Returns(Task.FromResult(pendingOperation));
            _transferContractService.Setup(x => x.GetBalanceOnAdapter(pendingOperation.CoinAdapterAddress, pendingOperation.FromAddress))
                .Returns(Task.FromResult(new BigInteger(0)));
            _coinEventService.Setup(x => x.PublishEvent(It.IsAny<ICoinEvent>(), It.IsAny<bool>())).Returns(Task.FromResult(0)).Verifiable();

            #endregion

            MonitoringOperationJob job = GetJob();
            await job.Execute(message, new Lykke.JobTriggers.Triggers.Bindings.QueueTriggeringContext(DateTimeOffset.UtcNow));

            _coinEventService.Verify(x => x.PublishEvent(It.IsAny<ICoinEvent>(), It.IsAny<bool>()), Times.Never);
        }

        //2.
        [TestMethod]
        public async Task MonitoringOperationJobUnitTest_ProcessOperation()
        {
            string operationId = Guid.NewGuid().ToString();
            string transactionHash = "0x10000000000000001";
            OperationHashMatchMessage message = new OperationHashMatchMessage()
            {
                OperationId = operationId,
                PutDateTime = DateTime.UtcNow,
                TransactionHash = null
            };

            #region Arrange Mocks
            IPendingOperation pendingOperation = new PendingOperation()
            {
                Amount = "1000000000000000000",
                FromAddress = "fromAddress",
                OperationId = operationId,
                OperationType = OperationTypes.Transfer,
                ToAddress = "toAddress",
                CoinAdapterAddress = "coinAdapter",
            };

            _pendingOperationService.Setup(x => x.GetOperationAsync(pendingOperation.OperationId)).Returns(Task.FromResult(pendingOperation));
            _transferContractService.Setup(x => x.GetBalanceOnAdapter(pendingOperation.CoinAdapterAddress, pendingOperation.FromAddress))
                .Returns(Task.FromResult(new BigInteger(1000000000000000001)));
            _coinEventService.Setup(x => x.PublishEvent(It.IsAny<ICoinEvent>(), It.IsAny<bool>())).Returns(Task.FromResult(0)).Verifiable();
            _exchangeContractService.Setup(x => x.Transfer(Guid.Parse(operationId), pendingOperation.CoinAdapterAddress,
                            pendingOperation.FromAddress,
                            pendingOperation.ToAddress, BigInteger.Parse(pendingOperation.Amount), pendingOperation.SignFrom))
                            .Returns(Task.FromResult(transactionHash)).Verifiable();

            #endregion

            MonitoringOperationJob job = GetJob();
            await job.Execute(message, new Lykke.JobTriggers.Triggers.Bindings.QueueTriggeringContext(DateTimeOffset.UtcNow));
            _exchangeContractService.Verify(x => x.Transfer(Guid.Parse(operationId), pendingOperation.CoinAdapterAddress,
                            pendingOperation.FromAddress,
                            pendingOperation.ToAddress, BigInteger.Parse(pendingOperation.Amount), pendingOperation.SignFrom), Times.Once);
            _coinEventService.Verify(x => x.PublishEvent(It.IsAny<ICoinEvent>(), It.IsAny<bool>()), Times.Once);
        }

        private MonitoringOperationJob GetJob()
        {
            return new MonitoringOperationJob(_log.Object,
                _baseSettings.Object,
                _pendingOperationService.Object,
                _exchangeContractService.Object,
                _coinEventService.Object,
                _transferContractService.Object,
                _eventTraceRepository.Object,
                _queueFactory.Object
                );
        }
    }
}

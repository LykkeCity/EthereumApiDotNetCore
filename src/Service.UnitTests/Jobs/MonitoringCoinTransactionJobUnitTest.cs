using Lykke.Service.EthereumCore.AzureRepositories.Repositories;
using Common.Log;
using Lykke.Service.EthereumCore.Core.Notifiers;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Job.EthereumCore.Job;
using Lykke.JobTriggers.Triggers.Bindings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.EthereumCore.Services.Coins;
using Lykke.Service.EthereumCore.Services.Coins.Models;
using Lykke.Service.EthereumCore.Services.New;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.UnitTests.Jobs
{
    /// <summary>
    /// Consider following test cases:
    /// 1. Transaction is in memory pool -> skip execution
    /// 2. Transaction is in blockchain -> send event(success)
    /// 3. Transaction is not in memory pool nor in blockchain(repeat operation)
    /// </summary>
    [TestClass]
    public class MonitoringCoinTransactionJobUnitTest
    {
        private string _memoryPoolTransactionHash = "0x1000000000000000";
        private string _commitedTransactionHash =   "0x2000000000000000";
        private string _lostTransactionHash =       "0x3000000000000000";

        private Mock<ILog> _log;
        private Mock<ICoinTransactionService> _coinTransactionService;
        private Mock<IBaseSettings> _settings;
        private Mock<ISlackNotifier> _slackNotifier;
        private Mock<ICoinEventService> _coinEventService;
        private Mock<IPendingTransactionsRepository> _pendingTransactionsRepository;
        private Mock<IPendingOperationService> _pendingOperationService;
        private Mock<ITransactionEventsService> _transactionEventsService;
        private Mock<IEventTraceRepository> _eventTraceRepository;
        private Mock<IUserTransferWalletRepository> _userTransferWalletRepository;
        private Mock<IEthereumTransactionService> _ethereumTransactionService;

        [TestInitialize]
        public void Test()
        {
            _log = new Mock<ILog>();
            _coinTransactionService = new Mock<ICoinTransactionService>();
            _settings = new Mock<IBaseSettings>();
            _slackNotifier = new Mock<ISlackNotifier>();
            _coinEventService = new Mock<ICoinEventService>();
            _pendingTransactionsRepository = new Mock<IPendingTransactionsRepository>();
            _pendingOperationService = new Mock<IPendingOperationService>();
            _transactionEventsService = new Mock<ITransactionEventsService>();
            _eventTraceRepository = new Mock<IEventTraceRepository>();
            _userTransferWalletRepository = new Mock<IUserTransferWalletRepository>();
            _ethereumTransactionService = new Mock<IEthereumTransactionService>();
        }

        //1.
        [TestMethod]
        public async Task MonitoringCoinTransactionJobUnitTest_SkipTransactionThatIsStillInMemoryPool()
        {
            string operationId = "OpId-1";
            QueueTriggeringContext context = new QueueTriggeringContext(DateTimeOffset.UtcNow);
            CoinTransactionMessage coinTransactionMessage = new CoinTransactionMessage()
            {
                PutDateTime = DateTime.UtcNow,
                TransactionHash = _memoryPoolTransactionHash,
                OperationId = operationId
            };

            #region ArrangeMocks

            _ethereumTransactionService.Setup(x => x.IsTransactionInPool(_memoryPoolTransactionHash)).Returns(Task.FromResult(true));
            _coinTransactionService.Setup(x => x.ProcessTransaction(coinTransactionMessage)).Verifiable();

            #endregion

            MonitoringCoinTransactionJob job = GetJob();
            await job.Execute(coinTransactionMessage, context);

            _coinTransactionService.Verify(x => x.ProcessTransaction(coinTransactionMessage), Times.Never);
        }

        //2.
        [TestMethod]
        public async Task MonitoringCoinTransactionJobUnitTest_TransactionIsInBlockchainSenhdEvent()
        {
            string operationId = "OpId-2";
            QueueTriggeringContext context = new QueueTriggeringContext(DateTimeOffset.UtcNow);
            CoinTransactionMessage coinTransactionMessage = new CoinTransactionMessage()
            {
                PutDateTime = DateTime.UtcNow,
                TransactionHash = _commitedTransactionHash,
                OperationId = operationId
            };

            #region ArrangeMocks
            ICoinTransaction coinTransaction = new CoinTransaction()
            {
                ConfirmationLevel = 1,
                TransactionHash = _commitedTransactionHash
            };
            ICoinEvent coinEvent = new CoinEvent(operationId,
                _commitedTransactionHash, "from", "to", "1000000000000000000", CoinEventType.TransferStarted, "contractAddress", true);
            _ethereumTransactionService.Setup(x => x.IsTransactionInPool(_commitedTransactionHash)).Returns(Task.FromResult(false));
            _coinTransactionService.Setup(x => x.ProcessTransaction(coinTransactionMessage)).Returns(Task.FromResult<ICoinTransaction>(coinTransaction));
            _coinEventService.Setup(x => x.GetCoinEvent(_commitedTransactionHash)).Returns(Task.FromResult<ICoinEvent>(coinEvent));
            _coinEventService.Setup(x => x.PublishEvent(coinEvent, It.IsAny<bool>())).Returns(Task.FromResult(0)).Verifiable();

            #endregion

            MonitoringCoinTransactionJob job = GetJob();
            await job.Execute(coinTransactionMessage, context);

            _coinEventService.Verify(x => x.PublishEvent(coinEvent, It.IsAny<bool>()), Times.Once);
        }

        //3.
        [TestMethod]
        public async Task MonitoringCoinTransactionJobUnitTest_LostTransactionProcessing()
        {
            string operationId = "OpId-3";
            QueueTriggeringContext context = new QueueTriggeringContext(DateTimeOffset.UtcNow);
            CoinTransactionMessage coinTransactionMessage = new CoinTransactionMessage()
            {
                PutDateTime = DateTime.UtcNow,
                TransactionHash = _lostTransactionHash,
                OperationId = operationId
            };

            #region ArrangeMocks

            ICoinEvent coinEvent = new CoinEvent(operationId,
                _lostTransactionHash, "from", "to", "1000000000000000000", CoinEventType.TransferStarted, "contractAddress", true);
            _ethereumTransactionService.Setup(x => x.IsTransactionInPool(_lostTransactionHash)).Returns(Task.FromResult(false));
            _coinTransactionService.Setup(x => x.ProcessTransaction(coinTransactionMessage)).Returns(Task.FromResult<ICoinTransaction>(null));
            _coinEventService.Setup(x => x.GetCoinEvent(_lostTransactionHash)).Returns(Task.FromResult<ICoinEvent>(coinEvent));
            _coinEventService.Setup(x => x.PublishEvent(coinEvent, It.IsAny<bool>())).Returns(Task.FromResult(0)).Verifiable();
            _pendingOperationService.Setup(x => x.RefreshOperationByIdAsync(operationId)).Returns(Task.FromResult(0)).Verifiable();

            #endregion

            MonitoringCoinTransactionJob job = GetJob();
            await job.Execute(coinTransactionMessage, context);

            _coinEventService.Verify(x => x.PublishEvent(coinEvent, It.IsAny<bool>()), Times.Never);
            _pendingOperationService.Verify(x => x.RefreshOperationByIdAsync(operationId), Times.Once);
        }

        private MonitoringCoinTransactionJob GetJob()
        {
           return new MonitoringCoinTransactionJob(_log.Object,
                _coinTransactionService.Object,
                _settings.Object,
                _slackNotifier.Object,
                _coinEventService.Object,
                _pendingTransactionsRepository.Object,
                _pendingOperationService.Object,
                _transactionEventsService.Object,
                _eventTraceRepository.Object,
                _userTransferWalletRepository.Object,
                _ethereumTransactionService.Object);
        }
    }
}

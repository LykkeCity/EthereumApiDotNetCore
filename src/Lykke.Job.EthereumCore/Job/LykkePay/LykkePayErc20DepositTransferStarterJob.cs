using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Nethereum.Web3;
using Lykke.Service.EthereumCore.Services;
using Common.Log;
using Lykke.Service.EthereumCore.Core.Settings;
using System.Numerics;
using System;
using Autofac.Features.AttributeFilters;
using AzureStorage.Queue;
using Common;
using Lykke.Job.EthereumCore.Contracts.Enums.LykkePay;
using Lykke.Job.EthereumCore.Contracts.Events.LykkePay;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.Messages.LykkePay;
using Lykke.Service.EthereumCore.Core.Services;
using Lykke.Service.EthereumCore.Core.Shared;
using Lykke.Service.EthereumCore.Services.Coins.Models;
using Lykke.Service.EthereumCore.Services.HotWallet;
using Lykke.Service.RabbitMQ;

namespace Lykke.Job.EthereumCore.Job
{
    public class LykkePayErc20DepositTransferStarterJob
    {
        private readonly ILog _logger;

        private readonly AppSettings _settings;
        private readonly IWeb3 _web3;
        private readonly IHotWalletOperationRepository _operationsRepository;
        private readonly IQueueExt _transactionMonitoringQueue;
        private readonly IHotWalletTransactionRepository _hotWalletTransactionRepository;
        private readonly IRabbitQueuePublisher _rabbitQueuePublisher;
        private readonly IErcInterfaceService _ercInterfaceService;
        private readonly IQueueExt _transactionStartedNotificationQueue;

        public LykkePayErc20DepositTransferStarterJob(AppSettings settings,
            ILog logger,
            IWeb3 web3,
            IQueueFactory queueFactory,
            [KeyFilter(Constants.LykkePayKey)]IHotWalletOperationRepository operationsRepository,
            [KeyFilter(Constants.LykkePayKey)]IHotWalletTransactionRepository hotWalletTransactionRepository,
            IRabbitQueuePublisher rabbitQueuePublisher,
            IErcInterfaceService ercInterfaceService
            )
        {
            _settings = settings;
            _logger = logger;
            _web3 = web3;
            _operationsRepository = operationsRepository;
            _transactionMonitoringQueue = queueFactory.Build(Constants.LykkePayTransactionMonitoringQueue);
            _transactionStartedNotificationQueue = queueFactory.Build(Constants.LykkePayErc223TransferNotificationsQueue);
            _hotWalletTransactionRepository = hotWalletTransactionRepository;
            _rabbitQueuePublisher = rabbitQueuePublisher;
            _ercInterfaceService = ercInterfaceService;
        }

        [QueueTrigger(Constants.LykkePayErc223TransferQueue, 200, true)]
        public async Task Execute(LykkePayErc20TransferMessage transaction, QueueTriggeringContext context)
        {
            IHotWalletOperation operation = null;

            if (string.IsNullOrEmpty(transaction?.OperationId))
            {
                await _logger.WriteWarningAsync(nameof(LykkePayErc20DepositTransferStarterJob),
                    "Execute", "", "Empty message skipped");

                return;
            }

            try
            {
                operation = await _operationsRepository.GetAsync(transaction.OperationId);

                if (operation == null)
                {
                    await _logger.WriteWarningAsync(nameof(LykkePayErc20DepositTransferStarterJob),
                        "Execute", transaction.ToJson(),
                        $"No operation for id {transaction?.OperationId} message skipped");

                    return;
                }

                var transactionSenderAddress = _settings.LykkePay.LykkePayAddress;
                var balance =
                    await _ercInterfaceService.GetPendingBalanceForExternalTokenAsync(operation.FromAddress,
                        operation.TokenAddress);
                if (balance == 0)
                {
                    await _logger.WriteWarningAsync(nameof(LykkePayErc20DepositTransferStarterJob),
                        "Execute", transaction.ToJson(),
                        $"DepositAddress: {operation.FromAddress}, TokenAddress: {operation.TokenAddress}");

                    //TODO: Transaction Failed

                    return;
                }

                var trHash = await Erc20SharedService.StartDepositTransferAsync(_web3, _settings.EthereumCore,
                    transactionSenderAddress,
                    operation.FromAddress, operation.TokenAddress, operation.ToAddress);
                await _hotWalletTransactionRepository.SaveAsync(new HotWalletCashoutTransaction()
                {
                    OperationId = transaction.OperationId,
                    TransactionHash = trHash
                });

                var message = new CoinTransactionMessage()
                {
                    OperationId = transaction.OperationId,
                    TransactionHash = trHash
                };

                //Observe transaction
                await _transactionMonitoringQueue.PutRawMessageAsync(
                    Newtonsoft.Json.JsonConvert.SerializeObject(message));

                var notificationMessage = new LykkePayErc20TransferNotificationMessage()
                {
                    OperationId = transaction.OperationId,
                    TransactionHash = trHash,
                    Balance = balance.ToString() //At the starting moment(may change at the end of the execution)
                };

                await _transactionStartedNotificationQueue.PutRawMessageAsync(
                    Newtonsoft.Json.JsonConvert.SerializeObject(notificationMessage));
            }
            catch (ClientSideException ex)
            {
                if (operation == null)
                    return;

                TransferEvent @event = new TransferEvent(transaction.OperationId,
                    "",
                    operation.Amount.ToString(),
                    operation.TokenAddress,
                    operation.FromAddress,
                    operation.ToAddress,
                    "",
                    0,
                    SenderType.EthereumCore,
                    EventType.Failed,
                    WorkflowType.LykkePay,
                    DateTime.UtcNow);

                await _logger.WriteWarningAsync(nameof(LykkePayErc20DepositTransferStarterJob), "Execute",
                    operation.ToJson(), ex);

                await _rabbitQueuePublisher.PublshEvent(@event);
            }
            catch (Exception ex)
            {
                if (transaction == null)
                    return;

                if (ex.Message != transaction.LastError)
                    await _logger.WriteWarningAsync(nameof(LykkePayErc20DepositTransferStarterJob),
                        "Execute", transaction.ToJson(), "transaction.OperationId");

                transaction.LastError = ex.Message;
                transaction.DequeueCount++;
                context.MoveMessageToEnd(transaction.ToJson());
                context.SetCountQueueBasedDelay(_settings.EthereumCore.MaxQueueDelay, 200);

                await _logger.WriteErrorAsync(nameof(LykkePayErc20DepositTransferStarterJob), "Execute",
                    transaction.ToJson(), ex);
            }
        }
    }
}

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
    public class LykkePayTransferNotificationJob
    {
        private readonly ILog _logger;

        private readonly AppSettings _settings;
        private readonly IWeb3 _web3;
        private readonly IHotWalletOperationRepository _operationsRepository;
        private readonly IQueueExt _transactionMonitoringQueue;
        private readonly IHotWalletTransactionRepository _hotWalletTransactionRepository;
        private readonly IRabbitQueuePublisher _rabbitQueuePublisher;
        private readonly IErcInterfaceService _ercInterfaceService;

        public LykkePayTransferNotificationJob(AppSettings settings,
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
            _hotWalletTransactionRepository = hotWalletTransactionRepository;
            _rabbitQueuePublisher = rabbitQueuePublisher;
            _ercInterfaceService = ercInterfaceService;
        }

        [QueueTrigger(Constants.LykkePayErc223TransferNotificationsQueue, 200, true)]
        public async Task Execute(LykkePayErc20TransferNotificationMessage message, QueueTriggeringContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(message?.OperationId))
                {
                    await _logger.WriteWarningAsync(nameof(LykkePayErc20DepositTransferStarterJob),
                        "Execute", "", "Empty message skipped");

                    return;
                }

                var operation = await _operationsRepository.GetAsync(message.OperationId);

                if (operation == null)
                {
                    await _logger.WriteWarningAsync(nameof(LykkePayErc20DepositTransferStarterJob),
                        "Execute", message.ToJson(), $"No operation for id {message?.OperationId} message skipped");

                    return;
                }

                TransferEvent @event = new TransferEvent(operation.OperationId,
                    message.TransactionHash,
                    message.Balance,
                    operation.TokenAddress,
                    operation.FromAddress,
                    operation.ToAddress,
                    SenderType.EthereumCore,
                    EventType.Started);

                await _rabbitQueuePublisher.PublshEvent(@event);
            }
            catch (Exception ex)
            {
                if (message == null)
                    return;

                if (ex.Message != message.LastError)
                    await _logger.WriteWarningAsync(nameof(LykkePayTransferNotificationJob),
                        "Execute", message.ToJson(), "transaction.OperationId");

                message.LastError = ex.Message;
                message.DequeueCount++;
                context.MoveMessageToEnd(message.ToJson());
                context.SetCountQueueBasedDelay(_settings.EthereumCore.MaxQueueDelay, 200);

                await _logger.WriteErrorAsync(nameof(LykkePayTransferNotificationJob), "Execute", message.ToJson(), ex);
            }
        }
    }
}

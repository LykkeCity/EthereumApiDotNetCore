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
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using Lykke.Service.RabbitMQ;
using Nethereum.RPC.Eth.DTOs;

namespace Lykke.Job.EthereumCore.Job
{
    public class LykkePayTransferNotificationJob
    {
        private readonly ILog _logger;

        private readonly AppSettings _settings;
        private readonly IHotWalletOperationRepository _operationsRepository;
        private readonly IRabbitQueuePublisher _rabbitQueuePublisher;
        private readonly IEthereumIndexerService _ethereumIndexerService;
        private readonly IWeb3 _web3;

        public LykkePayTransferNotificationJob(AppSettings settings,
            ILog logger,
            [KeyFilter(Constants.LykkePayKey)]IHotWalletOperationRepository operationsRepository,
            IRabbitQueuePublisher rabbitQueuePublisher,
            IWeb3 web3
            )
        {
            _settings = settings;
            _logger = logger;
            _operationsRepository = operationsRepository;
            _rabbitQueuePublisher = rabbitQueuePublisher;
            _web3 = web3;
        }

        [QueueTrigger(Constants.LykkePayErc223TransferNotificationsQueue, 200, true)]
        public async Task Execute(LykkePayErc20TransferNotificationMessage message, QueueTriggeringContext context)
        {
            if (string.IsNullOrEmpty(message?.OperationId))
            {
                await _logger.WriteWarningAsync(nameof(LykkePayErc20DepositTransferStarterJob),
                    "Execute", "", "Empty message skipped");

                return;
            }

            try
            {
                var operation = await _operationsRepository.GetAsync(message.OperationId);

                if (operation == null)
                {
                    await _logger.WriteWarningAsync(nameof(LykkePayErc20DepositTransferStarterJob),
                        "Execute", message.ToJson(), $"No operation for id {message?.OperationId} message skipped");

                    return;
                }

                Transaction transaction = await _web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(message.TransactionHash);

                if (transaction == null)
                {
                    message.LastError = "Not yet indexed";
                    message.DequeueCount++;
                    context.MoveMessageToEnd(message.ToJson());
                    context.SetCountQueueBasedDelay(_settings.EthereumCore.MaxQueueDelay, 30000);
                    return;
                }

                TransferEvent @event = new TransferEvent(operation.OperationId,
                    message.TransactionHash,
                    message.Balance,
                    operation.TokenAddress,
                    operation.FromAddress,
                    operation.ToAddress,
                    transaction?.BlockHash,
                    (ulong)transaction?.BlockNumber.Value,
                    SenderType.EthereumCore,
                    EventType.Started,
                    WorkflowType.LykkePay,
                    DateTime.UtcNow);

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

using System;
using System.Numerics;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using AzureStorage.Queue;
using Common;
using Common.Log;
using Lykke.Job.EthereumCore.Contracts.Enums.LykkePay;
using Lykke.Job.EthereumCore.Contracts.Events.LykkePay;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Airlines;
using Lykke.Service.EthereumCore.Core.LykkePay;
using Lykke.Service.EthereumCore.Core.Messages.LykkePay;
using Lykke.Service.EthereumCore.Core.Notifiers;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Core.Shared;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.EthereumCore.Services.Coins;
using Lykke.Service.EthereumCore.Services.Coins.Models;
using Lykke.Service.RabbitMQ;

namespace Lykke.Job.EthereumCore.Job.Airlines
{
    public class AirlinesHotWalletMonitoringTransactionJob
    {
        private readonly ILog _log;
        private readonly ICoinTransactionService _coinTransactionService;
        private readonly AppSettings _settings;
        private readonly ISlackNotifier _slackNotifier;
        private readonly IHotWalletTransactionRepository _hotWalletCashoutTransactionRepository;
        private readonly IHotWalletOperationRepository _hotWalletCashoutRepository;
        private readonly IRabbitQueuePublisher _rabbitQueuePublisher;
        private readonly ILykkePayEventsService _transactionEventsService;
        private readonly IEthereumTransactionService _ethereumTransactionService;
        private readonly IUserTransferWalletRepository _userTransferWalletRepository;
        private readonly IAirlinesErc20DepositContractService _erc20DepositContractService;
        private IQueueExt _transferStartQueue;

        public AirlinesHotWalletMonitoringTransactionJob(ILog log,
            ICoinTransactionService coinTransactionService,
            AppSettings settings,
            ISlackNotifier slackNotifier,
            IEthereumTransactionService ethereumTransactionService,
            [KeyFilter(Constants.AirLinesKey)]IHotWalletTransactionRepository hotWalletCashoutTransactionRepository,
            [KeyFilter(Constants.AirLinesKey)]IHotWalletOperationRepository hotWalletCashoutRepository,
            IRabbitQueuePublisher rabbitQueuePublisher,
            ILykkePayEventsService transactionEventsService,
            IUserTransferWalletRepository userTransferWalletRepository,
            [KeyFilter(Constants.AirLinesKey)]IAirlinesErc20DepositContractService erc20DepositContractService,
            IQueueFactory queueFactory)
        {
            _transactionEventsService = transactionEventsService;
            _ethereumTransactionService = ethereumTransactionService;
            _settings = settings;
            _log = log;
            _coinTransactionService = coinTransactionService;
            _slackNotifier = slackNotifier;
            _hotWalletCashoutTransactionRepository = hotWalletCashoutTransactionRepository;
            _hotWalletCashoutRepository = hotWalletCashoutRepository;
            _rabbitQueuePublisher = rabbitQueuePublisher;
            _userTransferWalletRepository = userTransferWalletRepository;
            _erc20DepositContractService = erc20DepositContractService;
            _transferStartQueue = queueFactory.Build(Constants.AirlinesErc223TransferQueue);
        }

        [QueueTrigger(Constants.AirlinesTransactionMonitoringQueue, 100, true)]
        public async Task Execute(CoinTransactionMessage transaction, QueueTriggeringContext context)
        {
            ICoinTransaction coinTransaction = null;
            try
            {
                bool isTransactionInMemoryPool = await _ethereumTransactionService.IsTransactionInPool(transaction.TransactionHash);
                if (isTransactionInMemoryPool)
                {
                    SendMessageToTheQueueEnd(context, transaction, 100, "Transaction is in memory pool");
                    return;
                }

                coinTransaction = await _coinTransactionService.ProcessTransaction(transaction);
            }
            catch (Exception ex)
            {
                if (ex.Message != transaction.LastError)
                    await _log.WriteWarningAsync(nameof(AirlinesHotWalletMonitoringTransactionJob), "Execute", $"TrHash: [{transaction.TransactionHash}]", "");

                SendMessageToTheQueueEnd(context, transaction, 200, ex.Message);

                await _log.WriteErrorAsync(nameof(AirlinesHotWalletMonitoringTransactionJob), "Execute", "", ex);
                return;
            }

            if (coinTransaction == null)
            {
                await RepeatOperationTillWin(transaction);
                await _slackNotifier.ErrorAsync($"Airlines: Transaction with hash {transaction.TransactionHash} has ERROR. RETRY. Address is yet blocked");
            }
            else
            {
                if (coinTransaction.ConfirmationLevel >= CoinTransactionService.Level2Confirm)
                {
                    bool sentToRabbit = await SendCompleteEvent(transaction.TransactionHash, transaction.OperationId, !coinTransaction.Error, context, transaction);

                    if (sentToRabbit)
                    {
                        await _log.WriteInfoAsync(nameof(AirlinesHotWalletMonitoringTransactionJob), "Execute", "",
                                   $"Put coin transaction {transaction.TransactionHash} to rabbit queue with confimation level {coinTransaction?.ConfirmationLevel ?? 0}");
                    }
                    else
                    {
                        await _log.WriteInfoAsync(nameof(AirlinesHotWalletMonitoringTransactionJob), "Execute", "",
                            $"Put coin transaction {transaction.TransactionHash} to monitoring queue with confimation level {coinTransaction?.ConfirmationLevel ?? 0}");
                    }

                    if (coinTransaction.Error)
                    {
                        await _slackNotifier.ErrorAsync($"EthereumCoreService: HOTWALLET - Transaction with hash {transaction.TransactionHash} has an Error. Notify Caller about fail!");
                    }
                }
                else
                {
                    SendMessageToTheQueueEnd(context, transaction, 100);
                    await _log.WriteInfoAsync(nameof(AirlinesHotWalletMonitoringTransactionJob), "Execute", "",
                            $"Put coin transaction {transaction.TransactionHash} to monitoring queue with confimation level {coinTransaction?.ConfirmationLevel ?? 0}");
                }
            }
        }

        private async Task<IHotWalletOperation> GetOperationAsync(string trHash, string operationId)
        {
            operationId = operationId ?? "";
            var cashoutTransaction = await _hotWalletCashoutTransactionRepository.GetByTransactionHashAsync(trHash) ??
                await _hotWalletCashoutTransactionRepository.GetByOperationIdAsync(operationId);
            var cashout = await _hotWalletCashoutRepository.GetAsync(cashoutTransaction?.OperationId);

            return cashout;
        }

        private async Task RepeatOperationTillWin(CoinTransactionMessage message)
        {
            var operation = await GetOperationAsync(message?.TransactionHash, message?.OperationId);

            if (operation == null)
                return;

            switch (operation.OperationType)
            {
                case HotWalletOperationType.Cashout:
                    break;

                case HotWalletOperationType.Cashin:
                    var retryMessage = new LykkePayErc20TransferMessage()
                    {
                        OperationId = operation.OperationId
                    };

                    await _transferStartQueue.PutRawMessageAsync(retryMessage.ToJson());
                    break;

                default:
                    return;
            }
        }

        //return whether we have sent to rabbit or not
        private async Task<bool> SendCompleteEvent(string transactionHash, string operationId, bool success, QueueTriggeringContext context, CoinTransactionMessage transaction)
        {
            try
            {
                var operation = await GetOperationAsync(transactionHash, operationId);
                if (operation == null)
                {
                    return false;
                }

                (BigInteger? amount, string blockHash, ulong blockNumber) transferedInfo = (null, null, 0);
                string amount = operation.Amount.ToString();
                switch (operation.OperationType)
                {
                    case HotWalletOperationType.Cashout:
                        break;
                    case HotWalletOperationType.Cashin:
                        //There will be nothing to index in failed event
                        if (success)
                        {
                            transferedInfo =
                                await _transactionEventsService.IndexCashinEventsForErc20TransactionHashAsync(
                                    transactionHash);
                            if (transferedInfo.amount == null ||
                                transferedInfo.amount == 0)
                            {
                                //Not yet indexed
                                SendMessageToTheQueueEnd(context, transaction, 10000);
                                return false;
                            }

                            amount = transferedInfo.amount.ToString();
                        }

                        break;
                    default:
                        return false;
                }

                EventType eventType = success ? EventType.Completed : EventType.Failed;
                TransferEvent @event = new TransferEvent(operation.OperationId,
                    transactionHash,
                    amount,
                    operation.TokenAddress,
                    operation.FromAddress,
                    operation.ToAddress,
                    transferedInfo.blockHash,
                    transferedInfo.blockNumber,
                    SenderType.EthereumCore,
                    eventType,
                    WorkflowType.Airlines,
                    DateTime.UtcNow);

                await _rabbitQueuePublisher.PublshEvent(@event);

                return true;
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(AirlinesHotWalletMonitoringTransactionJob), "SendCompletedCoinEvent", $"trHash: {transactionHash}", e, DateTime.UtcNow);
                SendMessageToTheQueueEnd(context, transaction, 100);

                return false;
            }
        }

        private void SendMessageToTheQueueEnd(QueueTriggeringContext context, CoinTransactionMessage transaction, int delay, string error = "")
        {
            transaction.DequeueCount++;
            transaction.LastError = string.IsNullOrEmpty(error) ? transaction.LastError : error;
            context.MoveMessageToEnd(transaction.ToJson());
            context.SetCountQueueBasedDelay(_settings.EthereumCore.MaxQueueDelay, delay);
        }
    }
}

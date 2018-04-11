using System;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Services.Coins;
using Common.Log;
using Common;
using Lykke.Job.EthereumCore.Contracts.Enums.LykkePay;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Services.Coins.Models;
using Lykke.JobTriggers.Triggers.Bindings;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Core.Notifiers;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.EthereumCore.Services.HotWallet;
using RabbitMQ;
using Lykke.Job.EthereumCore.Contracts.Events;
using Lykke.Job.EthereumCore.Contracts.Events.LykkePay;
using Lykke.Service.RabbitMQ;
using Lykke.Service.EthereumCore.Services.New;

namespace Lykke.Job.EthereumCore.Job
{
    public class LykkePayHotWalletMonitoringTransactionJob
    {
        private readonly ILog _log;
        private readonly ICoinTransactionService _coinTransactionService;
        private readonly IBaseSettings _settings;
        private readonly ISlackNotifier _slackNotifier;
        private readonly IHotWalletTransactionRepository _hotWalletCashoutTransactionRepository;
        private readonly IHotWalletOperationRepository _hotWalletCashoutRepository;
        private readonly IHotWalletService _hotWalletService;
        private readonly IRabbitQueuePublisher _rabbitQueuePublisher;
        private readonly ILykkePayEventsService _transactionEventsService;
        private readonly IEthereumTransactionService _ethereumTransactionService;
        private readonly ICashinEventRepository _cashinEventRepository;


        public LykkePayHotWalletMonitoringTransactionJob(ILog log,
            ICoinTransactionService coinTransactionService,
            IBaseSettings settings,
            ISlackNotifier slackNotifier,
            IEthereumTransactionService ethereumTransactionService,
            IHotWalletTransactionRepository hotWalletCashoutTransactionRepository,
            IHotWalletOperationRepository hotWalletCashoutRepository,
            IHotWalletService hotWalletService,
            IRabbitQueuePublisher rabbitQueuePublisher,
            ICashinEventRepository cashinEventRepository,
            ILykkePayEventsService transactionEventsService)
        {
            _transactionEventsService = transactionEventsService;
            _ethereumTransactionService = ethereumTransactionService;
            _settings = settings;
            _log = log;
            _coinTransactionService = coinTransactionService;
            _slackNotifier = slackNotifier;
            _hotWalletCashoutTransactionRepository = hotWalletCashoutTransactionRepository;
            _hotWalletCashoutRepository = hotWalletCashoutRepository;
            _hotWalletService = hotWalletService;
            _rabbitQueuePublisher = rabbitQueuePublisher;
            _cashinEventRepository = cashinEventRepository;
        }

        [QueueTrigger(Constants.LykkePayTransactionMonitoringQueue, 100, true)]
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
                    await _log.WriteWarningAsync(nameof(HotWalletMonitoringTransactionJob), "Execute", $"TrHash: [{transaction.TransactionHash}]", "");

                SendMessageToTheQueueEnd(context, transaction, 200, ex.Message);

                await _log.WriteErrorAsync(nameof(HotWalletMonitoringTransactionJob), "Execute", "", ex);
                return;
            }

            if (coinTransaction == null || coinTransaction.Error)
            {
                await RepeatOperationTillWin(transaction);
                await _slackNotifier.ErrorAsync($"EthereumCoreService: Transaction with hash {transaction.TransactionHash} has no confirmations.");
            }
            else
            {
                if (coinTransaction.ConfirmationLevel >= CoinTransactionService.Level2Confirm)
                {
                    if (!coinTransaction.Error)
                    {
                        bool sentToRabbit = await SendCompleteEvent(transaction.TransactionHash, transaction.OperationId, true, context, transaction);

                        if (sentToRabbit)
                        {
                            await _log.WriteInfoAsync(nameof(HotWalletMonitoringTransactionJob), "Execute", "",
                                       $"Put coin transaction {transaction.TransactionHash} to rabbit queue with confimation level {coinTransaction?.ConfirmationLevel ?? 0}");
                        }
                        else
                        {
                            await _log.WriteInfoAsync(nameof(HotWalletMonitoringTransactionJob), "Execute", "",
                                $"Put coin transaction {transaction.TransactionHash} to monitoring queue with confimation level {coinTransaction?.ConfirmationLevel ?? 0}");
                        }
                    }
                    else
                    {
                        await _slackNotifier.ErrorAsync($"EthereumCoreService: HOTWALLET - Transaction with hash {transaction.TransactionHash} has an Error!");
                        await RepeatOperationTillWin(transaction);
                        await _slackNotifier.ErrorAsync($"EthereumCoreService: HOTWALLET - Transaction with hash {transaction.TransactionHash} has an Error. RETRY!");
                    }
                }
                else
                {
                    SendMessageToTheQueueEnd(context, transaction, 100);
                    await _log.WriteInfoAsync(nameof(HotWalletMonitoringTransactionJob), "Execute", "",
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
            switch (operation.OperationType)
            {
                case HotWalletOperationType.Cashout:
                    break;

                case HotWalletOperationType.Cashin:
                    await _hotWalletService.RemoveCashinLockAsync(operation.TokenAddress, operation.FromAddress);
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

                string amount;
                Lykke.Job.EthereumCore.Contracts.Enums.LykkePay.EventType eventType;
                switch (operation.OperationType)
                {
                    case HotWalletOperationType.Cashout:
                        amount = operation.Amount.ToString();
                        break;
                    case HotWalletOperationType.Cashin:
                        await _hotWalletService.RemoveCashinLockAsync(operation.TokenAddress, operation.FromAddress);
                        var cashinEvent = await _cashinEventRepository.GetAsync(transactionHash);
                        if (cashinEvent == null)
                        {
                            var transferedTokens = await _transactionEventsService.IndexCashinEventsForErc20TransactionHashAsync(transactionHash);
                            if (transferedTokens == null || transferedTokens == 0)
                            {
                                //Not yet indexed
                                SendMessageToTheQueueEnd(context, transaction, 5000);
                                return false;
                            }

                            amount = transferedTokens.ToString();
                        }
                        else
                        {
                            amount = cashinEvent.Amount;
                        }

                        eventType = EventType.Completed;
                        break;
                    default:
                        return false;
                }

                TransferEvent @event = new TransferEvent(operation.OperationId, 
                    transactionHash,
                    amount,
                    operation.TokenAddress,
                    operation.FromAddress, 
                    operation.ToAddress,
                    SenderType.EthereumCore,
                    EventType.Completed);

                await _rabbitQueuePublisher.PublshEvent(@event);

                return true;
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(HotWalletMonitoringTransactionJob), "SendCompletedCoinEvent", $"trHash: {transactionHash}", e, DateTime.UtcNow);
                SendMessageToTheQueueEnd(context, transaction, 100);

                return false;
            }
        }

        private void SendMessageToTheQueueEnd(QueueTriggeringContext context, CoinTransactionMessage transaction, int delay, string error = "")
        {
            transaction.DequeueCount++;
            transaction.LastError = string.IsNullOrEmpty(error) ? transaction.LastError : error;
            context.MoveMessageToEnd(transaction.ToJson());
            context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, delay);
        }
    }
}

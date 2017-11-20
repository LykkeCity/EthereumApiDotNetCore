using System;
using System.Threading.Tasks;
using Services.Coins;
using Common.Log;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;
using Core;
using Services.Coins.Models;
using Lykke.JobTriggers.Triggers.Bindings;
using Core.Settings;
using Core.Notifiers;
using Core.Repositories;
using Services;
using Newtonsoft.Json;
using Services.New;

namespace EthereumJobs.Job
{
    public class HotWalletMonitoringTransactionJob
    {
        private readonly ILog _log;
        private readonly ICoinTransactionService _coinTransactionService;
        private readonly IBaseSettings _settings;
        private readonly ISlackNotifier _slackNotifier;
        private readonly ICoinEventService _coinEventService;
        private readonly IPendingTransactionsRepository _pendingTransactionsRepository;
        private readonly IPendingOperationService _pendingOperationService;
        private readonly ITransactionEventsService _transactionEventsService;
        private readonly IEventTraceRepository _eventTraceRepository;
        private readonly IUserTransferWalletRepository _userTransferWalletRepository;
        private readonly IHotWalletCashoutTransactionRepository _hotWalletCashoutTransactionRepository;
        private readonly IHotWalletCashoutRepository _hotWalletCashoutRepository;
        private readonly IEthereumTransactionService _ethereumTransactionService;

        public HotWalletMonitoringTransactionJob(ILog log,
            ICoinTransactionService coinTransactionService,
            IBaseSettings settings, ISlackNotifier slackNotifier,
            ITransactionEventsService transactionEventsService,
            IUserTransferWalletRepository userTransferWalletRepository,
            IEthereumTransactionService ethereumTransactionService,
            IHotWalletCashoutTransactionRepository hotWalletCashoutTransactionRepository,
            IHotWalletCashoutRepository hotWalletCashoutRepository)
        {
            _ethereumTransactionService = ethereumTransactionService;
            _transactionEventsService = transactionEventsService;
            _settings = settings;
            _log = log;
            _coinTransactionService = coinTransactionService;
            _slackNotifier = slackNotifier;
            _userTransferWalletRepository = userTransferWalletRepository;
            _hotWalletCashoutTransactionRepository = hotWalletCashoutTransactionRepository;
            _hotWalletCashoutRepository = hotWalletCashoutRepository;
        }

        [QueueTrigger(Constants.HotWalletCashoutTransactionMonitoringQueue, 100, true)]
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
                //await _slackNotifier.ErrorAsync($"EthereumCoreService: Transaction with hash {transaction.TransactionHash} has no confirmations." +
                //    $" Reason - unable to find transaction in txPool and in blockchain within {_broadcastMonitoringPeriodSeconds} seconds");
            }
            else
            {
                if (coinTransaction != null &&
                    coinTransaction.ConfirmationLevel >= CoinTransactionService.Level2Confirm)
                {
                    if (!coinTransaction.Error)
                    {
                        bool sentToRabbit = await SendCompletedCoinEvent(transaction.TransactionHash, transaction.OperationId, true, context, transaction);

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
                        IHotWalletCashout coinEvent = await GetCashoutOperation(transaction.TransactionHash, transaction.OperationId);
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

        private async Task<IHotWalletCashout> GetCashoutOperation(string trHash, string operationId)
        {
            var cashoutTransaction = await _hotWalletCashoutTransactionRepository.GetByTransactionHashAsync(trHash) ??
                await _hotWalletCashoutTransactionRepository.GetByOperationIdAsync(operationId);
            var cashout = await _hotWalletCashoutRepository.GetAsync(cashoutTransaction?.OperationId);

            return cashout;
        }

        private async Task RepeatOperationTillWin(CoinTransactionMessage message)
        {
        }

        //return whether we have sent to rabbit or not
        private async Task<bool> SendCompletedCoinEvent(string transactionHash, string operationId, bool success, QueueTriggeringContext context, CoinTransactionMessage transaction)
        {
            try
            {
                
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

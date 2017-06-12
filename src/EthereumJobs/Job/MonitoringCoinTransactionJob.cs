﻿using System;
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
    public class MonitoringCoinTransactionJob
    {
        private readonly ILog _log;
        private readonly ICoinTransactionService _coinTransactionService;
        private readonly IBaseSettings _settings;
        private readonly ISlackNotifier _slackNotifier;
        private readonly ICoinEventService _coinEventService;
        private readonly IPendingTransactionsRepository _pendingTransactionsRepository;
        private readonly IPendingOperationService _pendingOperationService;
        private readonly ITransactionEventsService _transactionEventsService;

        public MonitoringCoinTransactionJob(ILog log, ICoinTransactionService coinTransactionService,
            IBaseSettings settings, ISlackNotifier slackNotifier, ICoinEventService coinEventService,
            IPendingTransactionsRepository pendingTransactionsRepository,
            IPendingOperationService pendingOperationService,
            ITransactionEventsService transactionEventsService)
        {
            _transactionEventsService = transactionEventsService;
            _settings = settings;
            _log = log;
            _coinTransactionService = coinTransactionService;
            _slackNotifier = slackNotifier;
            _coinEventService = coinEventService;
            _pendingTransactionsRepository = pendingTransactionsRepository;
            _pendingOperationService = pendingOperationService;
        }

        [QueueTrigger(Constants.TransactionMonitoringQueue, 100, true)]
        public async Task Execute(CoinTransactionMessage transaction, QueueTriggeringContext context)
        {
            ICoinTransaction coinTransaction = null;
            try
            {
                coinTransaction = await _coinTransactionService.ProcessTransaction(transaction);
            }
            catch (Exception ex)
            {
                if (ex.Message != transaction.LastError)
                    await _log.WriteWarningAsync("MonitoringCoinTransactionJob", "Execute", $"TrHash: [{transaction.TransactionHash}]", "");

                transaction.LastError = ex.Message;
                transaction.DequeueCount++;
                context.MoveMessageToEnd(transaction.ToJson());
                context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, 200);

                await _log.WriteErrorAsync("MonitoringCoinTransactionJob", "Execute", "", ex);
                return;
            }

            if ((coinTransaction == null || (coinTransaction.Error || coinTransaction.ConfirmationLevel == 0)) &&
                DateTime.UtcNow - transaction.PutDateTime > TimeSpan.FromSeconds(_settings.BroadcastMonitoringPeriodSeconds))
            {
                await RepeatOperationTillWin(transaction.TransactionHash);
                await _slackNotifier.ErrorAsync($"EthereumCoreService: Transaction with hash {transaction.TransactionHash} has no confirmations. Reason - timeout");
                //await SendCompletedCoinEvent(transaction.TransactionHash, false);
            }
            else
            {
                if (coinTransaction != null && coinTransaction.ConfirmationLevel != 0)
                {
                    bool sentToRabbit = await SendCompletedCoinEvent(transaction.TransactionHash, true, context);

                    if (sentToRabbit)
                    {
                        await _log.WriteInfoAsync("CoinTransactionService", "Execute", "",
                                   $"Put coin transaction {transaction.TransactionHash} to rabbit queue with confimation level {coinTransaction?.ConfirmationLevel ?? 0}");
                    }
                    else
                    {
                        await _log.WriteInfoAsync("CoinTransactionService", "Execute", "",
                            $"Put coin transaction {transaction.TransactionHash} to monitoring queue with confimation level {coinTransaction?.ConfirmationLevel ?? 0}");
                    }
                }
                else
                {
                    context.MoveMessageToEnd();
                    context.SetCountQueueBasedDelay(10000, 100);
                    await _log.WriteInfoAsync("CoinTransactionService", "Execute", "",
                            $"Put coin transaction {transaction.TransactionHash} to monitoring queue with confimation level {coinTransaction?.ConfirmationLevel ?? 0}");
                }

            }
        }

        private async Task RepeatOperationTillWin(string hash)
        {
            var pendingOp = await _pendingOperationService.GetOperationByHashAsync(hash);
            if (pendingOp == null)
            {
                //ignore because that mean that cashin somehow failed
                return;
            }

            await _pendingOperationService.RefreshOperationByIdAsync(pendingOp.OperationId);
        }

        //return whether we have sent to rabbit or not
        private async Task<bool> SendCompletedCoinEvent(string transactionHash, bool success, QueueTriggeringContext context)
        {
            try
            {
                var pendingOp = await _pendingOperationService.GetOperationByHashAsync(transactionHash);
                var coinEvent = pendingOp != null ?
                    await _coinEventService.GetCoinEventById(pendingOp.OperationId) : await _coinEventService.GetCoinEvent(transactionHash);
                coinEvent.Success = success;
                coinEvent.TransactionHash = transactionHash;

                switch (coinEvent.CoinEventType)
                {
                    case CoinEventType.CashinStarted:
                        ICashinEvent cashinEvent = await _transactionEventsService.GetCashinEvent(transactionHash);
                        if (cashinEvent == null)
                        {
                            context.MoveMessageToEnd();
                            context.SetCountQueueBasedDelay(10000, 100);

                            return false;
                        }

                        coinEvent.Amount = cashinEvent.Amount;
                        coinEvent.CoinEventType++;
                        break;
                    case CoinEventType.CashoutStarted:
                    case CoinEventType.TransferStarted:
                        //Say that Event Is completed
                        coinEvent.CoinEventType++;
                        break;
                    default: break;
                }

                await _coinEventService.PublishEvent(coinEvent, false);
                await _pendingTransactionsRepository.Delete(transactionHash);

                return true;
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync("MonitoringCoinTransactionJob", "SendCompletedCoinEvent", $"trHash: {transactionHash}", e, DateTime.UtcNow);
                context.MoveMessageToEnd();
                context.SetCountQueueBasedDelay(10000, 100);

                return false;
            }
        }
    }
}

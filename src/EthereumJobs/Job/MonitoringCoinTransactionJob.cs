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
        private readonly IEventTraceRepository _eventTraceRepository;
        private readonly IUserTransferWalletRepository _userTransferWalletRepository;
        private readonly IEthereumTransactionService _ethereumTransactionService;
        private readonly TimeSpan _broadcastMonitoringPeriodSeconds;

        public MonitoringCoinTransactionJob(ILog log, ICoinTransactionService coinTransactionService,
            IBaseSettings settings, ISlackNotifier slackNotifier, ICoinEventService coinEventService,
            IPendingTransactionsRepository pendingTransactionsRepository,
            IPendingOperationService pendingOperationService,
            ITransactionEventsService transactionEventsService,
            IEventTraceRepository eventTraceRepository,
            IUserTransferWalletRepository userTransferWalletRepository,
            IEthereumTransactionService ethereumTransactionService)
        {
            _ethereumTransactionService = ethereumTransactionService;
            _transactionEventsService = transactionEventsService;
            _settings = settings;
            _log = log;
            _coinTransactionService = coinTransactionService;
            _slackNotifier = slackNotifier;
            _coinEventService = coinEventService;
            _pendingTransactionsRepository = pendingTransactionsRepository;
            _pendingOperationService = pendingOperationService;
            _eventTraceRepository = eventTraceRepository;
            _userTransferWalletRepository = userTransferWalletRepository;
            _broadcastMonitoringPeriodSeconds = TimeSpan.FromSeconds(_settings.BroadcastMonitoringPeriodSeconds);
        }

        [QueueTrigger(Constants.TransactionMonitoringQueue, 100, true)]
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
                    await _log.WriteWarningAsync("MonitoringCoinTransactionJob", "Execute", $"TrHash: [{transaction.TransactionHash}]", "");

                SendMessageToTheQueueEnd(context, transaction, 200, ex.Message);

                await _log.WriteErrorAsync("MonitoringCoinTransactionJob", "Execute", "", ex);
                return;
            }

            if ((coinTransaction == null || coinTransaction.Error || coinTransaction.ConfirmationLevel == 0) && 
                (DateTime.UtcNow - transaction.PutDateTime > _broadcastMonitoringPeriodSeconds))
            {
                await RepeatOperationTillWin(transaction);
                await _slackNotifier.ErrorAsync($"EthereumCoreService: Transaction with hash {transaction.TransactionHash} has no confirmations." +
                    $" Reason - unable to find transaction in txPool and in blockchain within {_broadcastMonitoringPeriodSeconds} seconds");
            }
            else
            {
                if (coinTransaction != null && coinTransaction.ConfirmationLevel != 0)
                {
                    if (!coinTransaction.Error)
                    {
                        bool sentToRabbit = await SendCompletedCoinEvent(transaction.TransactionHash, transaction.OperationId, true, context, transaction);

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
                        ICoinEvent coinEvent = await GetCoinEvent(transaction.TransactionHash, transaction.OperationId, true);
                        await _slackNotifier.ErrorAsync($"EthereumCoreService: Transaction with hash {transaction.TransactionHash} has an Error!({coinEvent.CoinEventType})");
                        if (coinEvent.CoinEventType == CoinEventType.CashoutStarted || 
                            coinEvent.CoinEventType == CoinEventType.CashoutCompleted)
                        {
                            //Drop cashout operation;
                            return;
                        }
                        else
                        {
                            await RepeatOperationTillWin(transaction);
                            await _slackNotifier.ErrorAsync($"EthereumCoreService: Transaction with hash {transaction.TransactionHash} has an Error. RETRY!({coinEvent.CoinEventType})");
                        }
                    }
                }
                else
                {
                    SendMessageToTheQueueEnd(context, transaction, 100);
                    await _log.WriteInfoAsync("CoinTransactionService", "Execute", "",
                            $"Put coin transaction {transaction.TransactionHash} to monitoring queue with confimation level {coinTransaction?.ConfirmationLevel ?? 0}");
                }
            }
        }

        private async Task RepeatOperationTillWin(CoinTransactionMessage message)
        {
            ICoinEvent coinEvent = await GetCoinEvent(message.TransactionHash, message.OperationId, true);

            if (coinEvent == null)
            {
                await _eventTraceRepository.InsertAsync(new EventTrace()
                {
                    Note = $"Operation processing is over. Put it in the garbage. With hash {message.TransactionHash}",
                    OperationId = message.OperationId,
                    TraceDate = DateTime.UtcNow
                });

                return;
            }
            switch (coinEvent.CoinEventType)
            {
                case CoinEventType.CashinStarted:
                case CoinEventType.CashinCompleted:
                    await UpdateUserTransferWallet(coinEvent.FromAddress, coinEvent.ToAddress);
                    return;
                default:
                    break;
            }

            await _eventTraceRepository.InsertAsync(new EventTrace()
            {
                Note = $"Operation With Id {coinEvent.OperationId} hash {message.TransactionHash} goes to {Constants.PendingOperationsQueue}",
                OperationId = message.OperationId,
                TraceDate = DateTime.UtcNow
            });

            await _pendingOperationService.RefreshOperationByIdAsync(coinEvent.OperationId);
        }

        //return whether we have sent to rabbit or not
        private async Task<bool> SendCompletedCoinEvent(string transactionHash, string operationId, bool success, QueueTriggeringContext context, CoinTransactionMessage transaction)
        {
            try
            {
                ICoinEvent coinEvent = await GetCoinEvent(transactionHash, operationId, success);

                switch (coinEvent.CoinEventType)
                {
                    case CoinEventType.CashinStarted:
                        ICashinEvent cashinEvent = await _transactionEventsService.GetCashinEvent(transactionHash);
                        if (cashinEvent == null)
                        {
                            SendMessageToTheQueueEnd(context, transaction, 100);

                            return false;
                        }

                        //transferContract - userAddress
                        await UpdateUserTransferWallet(coinEvent.FromAddress, coinEvent.ToAddress.ToLower());
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
                await _coinEventService.PublishEvent(coinEvent, putInProcessingQueue: false);
                await _pendingTransactionsRepository.Delete(transactionHash);
                await _pendingOperationService.MatchHashToOpId(transactionHash, coinEvent.OperationId);

                return true;
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync("MonitoringCoinTransactionJob", "SendCompletedCoinEvent", $"trHash: {transactionHash}", e, DateTime.UtcNow);
                SendMessageToTheQueueEnd(context, transaction, 100);

                return false;
            }
        }

        private async Task<ICoinEvent> GetCoinEvent(string transactionHash, string operationId, bool success)
        {
            var pendingOp = await _pendingOperationService.GetOperationByHashAsync(transactionHash);
            string opIdToSearch = pendingOp?.OperationId ?? operationId;
            var coinEvent = !string.IsNullOrEmpty(opIdToSearch) ?
                await _coinEventService.GetCoinEventById(opIdToSearch) : await _coinEventService.GetCoinEvent(transactionHash);
            coinEvent = coinEvent ?? await _coinEventService.GetCoinEvent(transactionHash);
            coinEvent.Success = success;
            coinEvent.TransactionHash = transactionHash;

            return coinEvent;
        }

        private async Task UpdateUserTransferWallet(string transferContractAddress, string userAddress)
        {
            await _userTransferWalletRepository.ReplaceAsync(new UserTransferWallet()
            {
                LastBalance = "",
                TransferContractAddress = transferContractAddress,
                UpdateDate = DateTime.UtcNow,
                UserAddress = userAddress
            });
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

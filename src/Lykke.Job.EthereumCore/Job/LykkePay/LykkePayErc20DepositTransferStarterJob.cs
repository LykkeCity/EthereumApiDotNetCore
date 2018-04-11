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
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Messages.LykkePay;
using Lykke.Service.EthereumCore.Core.Services;
using Lykke.Service.EthereumCore.Core.Shared;
using Lykke.Service.EthereumCore.Services.Coins.Models;
using Lykke.Service.EthereumCore.Services.HotWallet;

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

        public LykkePayErc20DepositTransferStarterJob(AppSettings settings,
            ILog logger,
            IWeb3 web3,
            IQueueFactory queueFactory,
            [KeyFilter(Constants.LykkePayKey)]IHotWalletOperationRepository operationsRepository,
            [KeyFilter(Constants.LykkePayKey)]IHotWalletTransactionRepository hotWalletTransactionRepository
            )
        {
            _settings = settings;
            _logger = logger;
            _web3 = web3;
            _operationsRepository = operationsRepository;
            _transactionMonitoringQueue = queueFactory.Build(Constants.LykkePayTransactionMonitoringQueue);
            _hotWalletTransactionRepository = hotWalletTransactionRepository;
        }

        [QueueTrigger(Constants.LykkePayErc223TransferQueue, 200, true)]
        public async Task Execute(LykkePayErc20TransferMessage transaction, QueueTriggeringContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(transaction?.OperationId))
                {
                    await _logger.WriteWarningAsync(nameof(LykkePayErc20DepositTransferStarterJob),
                        "Execute", "", "Empty message skipped");

                    return;
                }

                var operation = await _operationsRepository.GetAsync(transaction.OperationId);

                if (operation == null)
                {
                    await _logger.WriteWarningAsync(nameof(LykkePayErc20DepositTransferStarterJob),
                        "Execute", transaction.ToJson(), $"No operation for id {transaction?.OperationId} message skipped");

                    return;
                }

                var transactionSenderAddress = _settings.LykkePay.LykkePayAddress;
                var trHash = await Erc20SharedService.StartTransferAsync(_web3, _settings.EthereumCore, transactionSenderAddress,
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

                await _transactionMonitoringQueue.PutRawMessageAsync(Newtonsoft.Json.JsonConvert.SerializeObject(message));
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

                await _logger.WriteErrorAsync(nameof(LykkePayErc20DepositTransferStarterJob), "Execute", transaction.ToJson(), ex);
            }
        }
    }
}

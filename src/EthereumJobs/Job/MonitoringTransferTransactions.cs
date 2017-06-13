using System.Threading.Tasks;
using Core.Repositories;
using Nethereum.Web3;
using Services;
using Common.Log;
using Core.Settings;
using System.Numerics;
using System;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;
using Core;

namespace EthereumJobs.Job
{
    public class MonitoringTransferTransactions
    {
        private readonly ILog _logger;
        private readonly ITransferContractTransactionService _transferContractTransactionService;
        private readonly IBaseSettings _settings;

        public MonitoringTransferTransactions(IBaseSettings settings,
            ILog logger,
            ITransferContractTransactionService transferContractTransactionService
            )
        {
            _settings = settings;
            _logger = logger;
            _transferContractTransactionService = transferContractTransactionService;
        }

        [QueueTrigger(Constants.ContractTransferQueue, 100, true)]
        public async Task Execute(TransferContractTransaction transaction, QueueTriggeringContext context)
        {
            try
            {
                await _transferContractTransactionService.TransferToCoinContract(transaction);
            }
            catch (Exception ex)
            {
                if (ex.Message != transaction.LastError)
                    await _logger.WriteWarningAsync("MonitoringCoinTransactionJob", "Execute", $"ContractAddress: [{transaction.ContractAddress}]", "");

                transaction.LastError = ex.Message;

                if (transaction.DequeueCount >= 5)
                {
                    context.MoveMessageToPoison();
                }
                else
                {
                    transaction.DequeueCount++;
                    context.MoveMessageToEnd();
                    context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, 200);
                }
                await _logger.WriteErrorAsync("MonitoringTransferTransactions", "Execute", "", ex);
            }
        }
    }
}

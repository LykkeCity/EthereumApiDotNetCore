using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Nethereum.Web3;
using Lykke.Service.EthereumCore.Services;
using Common.Log;
using Lykke.Service.EthereumCore.Core.Settings;
using System.Numerics;
using System;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;
using Lykke.Service.EthereumCore.Core;

namespace Lykke.Job.EthereumCore.Job
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

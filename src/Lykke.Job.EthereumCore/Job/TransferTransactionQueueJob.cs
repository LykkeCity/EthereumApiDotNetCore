using System;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Services;
using Common.Log;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Service.EthereumCore.Core;
using Lykke.JobTriggers.Triggers.Bindings;
using Lykke.Service.EthereumCore.Core.Settings;

namespace Lykke.Job.EthereumCore.Job
{
    //Cashin
    public class TransferTransactionQueueJob
    {
        private readonly ITransferContractTransactionService _contractTransferTransactionService;
        private readonly ILog _log;
        private readonly IBaseSettings _settings;

        public TransferTransactionQueueJob(ITransferContractTransactionService contractTransferTransactionService, ILog log, IBaseSettings settings)
        {
            _contractTransferTransactionService = contractTransferTransactionService;
            _log = log;
            _settings = settings;
        }

        [QueueTrigger(Constants.ContractTransferQueue, 100, true)]
        public async Task Execute(TransferContractTransaction contractTransferTr, QueueTriggeringContext context)
        {
            try
            {
                await _contractTransferTransactionService.TransferToCoinContract(contractTransferTr);
            }
            catch (Exception ex)
            {
                if (ex.Message != contractTransferTr.LastError)
                    await _log.WriteWarningAsync("MonitoringCoinTransactionJob", "Execute", $"ContractAddress: [{contractTransferTr.ContractAddress}]", "");

                contractTransferTr.LastError = ex.Message;

                if (contractTransferTr.DequeueCount >= _settings.MaxDequeueCount)
                {
                    context.MoveMessageToPoison();
                }
                else
                {
                    contractTransferTr.DequeueCount++;
                    context.MoveMessageToEnd(contractTransferTr.ToJson());
                    context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, 200);
                }
                await _log.WriteErrorAsync("TransferTransactionQueueJob", "TransferTransactionQueue", "", ex);
            }
        }
    }
}

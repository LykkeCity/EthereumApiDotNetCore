using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Services;
using Common.Log;
using Common;

namespace EthereumJobs.Job
{
    public class TransferTransactionQueueJob : TimerPeriod
    {
        private readonly ITransferContractTransactionService _contractTransferTransactionService;
        private readonly ILog _log;
        private const int TimerPeriodSeconds = 2;


        public TransferTransactionQueueJob(ITransferContractTransactionService contractTransferTransactionService, ILog log)
            : base("TransferTransactionQueueJob", TimerPeriodSeconds * 1000, log)
        {
            _contractTransferTransactionService = contractTransferTransactionService;
            _log = log;
        }

        public override async Task Execute()
        {
            try
            {
                while (await _contractTransferTransactionService.CompleteTransfer() && Working)
                {
                }
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync("EthereumWebJob", "TransferTransactionQueue", "", ex);
            }
        }
    }
}

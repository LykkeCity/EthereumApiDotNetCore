using System.Threading.Tasks;
using Core.Repositories;
using Nethereum.Web3;
using Services;
using Common.Log;
using Core.Settings;
using System.Numerics;
using System;
using Common;

namespace EthereumJobs.Job
{
    public class MonitoringTransferTransactions : TimerPeriod
    {

        private const int TimerPeriodSeconds = 60;
        private const int AlertNotChangedBalanceCount = 3;

        private readonly ILog _logger;
        private readonly ITransferContractTransactionService _transferContractTransactionService;

        public MonitoringTransferTransactions(IBaseSettings settings,
            ILog logger,
            ITransferContractTransactionService transferContractTransactionService
            ) :
            base("MonitoringTransferTransactions", TimerPeriodSeconds * 1000, logger)
        {

            _logger = logger;
            _transferContractTransactionService = transferContractTransactionService;
        }

        public override async Task Execute()
        {
            try
            {
                while (await _transferContractTransactionService.CompleteTransfer() && Working)
                {
                }
            }
            catch (Exception ex)
            {
                await _logger.WriteErrorAsync("EthereumJob", "MonitoringTransferTransactions", "", ex);
            }
        }
    }
}

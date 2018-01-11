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
using Lykke.Service.EthereumCore.Services.HotWallet;

namespace Lykke.Job.EthereumCore.Job
{
    public class Erc20DepositMonitoringCashinTransactions
    {
        private readonly ILog _logger;
        private readonly IErc20DepositTransactionService _transferContractTransactionService;
        private readonly IHotWalletService _hotWalletService;
        private readonly IBaseSettings _settings;

        public Erc20DepositMonitoringCashinTransactions(IBaseSettings settings,
            ILog logger,
            IErc20DepositTransactionService erc20DepositTransactionService,
            IHotWalletService hotWalletService
            )
        {
            _settings = settings;
            _logger = logger;
            _transferContractTransactionService = erc20DepositTransactionService;
            _hotWalletService = hotWalletService;
        }

        [QueueTrigger(Constants.Erc20DepositCashinTransferQueue, 100, true)]
        public async Task Execute(Erc20DepositContractTransaction transaction, QueueTriggeringContext context)
        {
            try
            {
                await _transferContractTransactionService.TransferToCoinContract(transaction);
            }
            catch (Exception ex)
            {
                if (ex.Message != transaction.LastError)
                    await _logger.WriteWarningAsync(nameof(Erc20DepositMonitoringCashinTransactions), 
                        "Execute", 
                        $"ContractAddress: [{transaction.ContractAddress}]", "");

                transaction.LastError = ex.Message;

                if (transaction.DequeueCount >= 5)
                {
                    context.MoveMessageToPoison(transaction.ToJson());
                }
                else
                {
                    transaction.DequeueCount++;
                    context.MoveMessageToEnd(transaction.ToJson());
                    context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, 200);
                }
                await _logger.WriteErrorAsync(nameof(Erc20DepositMonitoringCashinTransactions), "Execute", "", ex);
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using Common.Log;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Service.EthereumCore.Core;
using Lykke.JobTriggers.Triggers.Bindings;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Services.HotWallet;
using Lykke.Service.EthereumCore.Core.Messages.HotWallet;
using Lykke.Service.EthereumCore.Services;

namespace Lykke.Job.EthereumCore.Job
{
    public class HotWalletCashoutJob
    {
        private readonly ILog _log;
        private readonly IBaseSettings _settings;
        private readonly IOperationsService _operationsService;
        private readonly IHotWalletService _hotWalletService;

        public HotWalletCashoutJob(
            ILog log,
            IBaseSettings settings,
            IOperationsService operationsService,
            IHotWalletService hotWalletService
            )
        {
            _log = log;
            _settings = settings;
            _operationsService = operationsService;
            _hotWalletService = hotWalletService;
        }

        [QueueTrigger(Constants.HotWalletCashoutQueue, 100, true)]
        public async Task Execute(HotWalletCashoutMessage cashoutMessage, QueueTriggeringContext context)
        {
            if (cashoutMessage == null || string.IsNullOrEmpty(cashoutMessage.OperationId))
            {
                await _log.WriteWarningAsync(nameof(HotWalletCashoutJob), "Execute", "", "message is empty");

                return;
            }

            try
            {
                if (_operationsService.IsOperationAborted(cashoutMessage.OperationId))
                {
                    await _log.WriteWarningAsync(nameof(HotWalletCashoutJob), "Execute", $"{cashoutMessage.OperationId}", "Operation aborted");
                    return;
                }

                await _hotWalletService.StartCashoutAsync(cashoutMessage.OperationId);
            }
            catch (Exception exc)
            {
                await _log.WriteErrorAsync(nameof(HotWalletCashoutJob), "Execute", $"{cashoutMessage.OperationId}", exc);
                cashoutMessage.LastError = exc.Message;
                cashoutMessage.DequeueCount++;
                context.MoveMessageToEnd(cashoutMessage.ToJson());
                context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, 200);
            }
        }
    }
}

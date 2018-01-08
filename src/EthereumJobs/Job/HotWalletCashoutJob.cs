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
using Services.New.Models;
using System.Numerics;
using Core.Exceptions;
using AzureStorage.Queue;
using Newtonsoft.Json;
using Services.HotWallet;
using Core.Messages.HotWallet;

namespace EthereumJobs.Job
{
    public class HotWalletCashoutJob
    {
        private readonly ILog _log;
        private readonly IBaseSettings _settings;
        private readonly IHotWalletService _hotWalletService;

        public HotWalletCashoutJob(
            ILog log,
            IBaseSettings settings,
            IHotWalletService hotWalletService
            )
        {
            _log = log;
            _settings = settings;
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

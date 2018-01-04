using System;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Services.Coins;
using Common.Log;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Services.Coins.Models;
using Lykke.JobTriggers.Triggers.Bindings;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Core.Notifiers;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.EthereumCore.Services.New.Models;
using System.Numerics;
using Lykke.Service.EthereumCore.Core.Exceptions;
using AzureStorage.Queue;
using Newtonsoft.Json;
using Lykke.Service.EthereumCore.Services.HotWallet;
using Lykke.Service.EthereumCore.Core.Messages.HotWallet;

namespace Lykke.Job.EthereumCore.Job
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

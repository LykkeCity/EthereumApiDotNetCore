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
using EdjCase.JsonRpc.Client;

namespace EthereumJobs.Job
{
    public class HotWalletCashoutJob
    {
        private readonly ILog _log;
        private readonly IBaseSettings _settings;

        public HotWalletCashoutJob(
            ILog log
            //IBaseSettings settings,
            )
        {
        }

        [QueueTrigger(Constants.HotWalletCashoutQueue, 100, true)]
        public async Task Execute(OperationHashMatchMessage opMessage, QueueTriggeringContext context)
        {
        }
    }
}

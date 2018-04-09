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
using Newtonsoft.Json;
using Lykke.Service.EthereumCore.Services.New;
using Lykke.Service.EthereumCore.Services.New.Models;

namespace Lykke.Job.EthereumCore.Job.LykkePay
{
    public class LykkePayIndexingJob
    {
        private readonly ILog _log;
        private readonly IBaseSettings _settings;
        private readonly ITransactionEventsService _transactionEventsService;

        public LykkePayIndexingJob(ILog log, 
            IBaseSettings settings, 
            ITransactionEventsService transactionEventsService)
        {
            _transactionEventsService = transactionEventsService;
            _settings = settings;
            _log = log;
        }

        [TimerTrigger("0.00:00:30")]
        public async Task Execute()
        {
            try
            {
                await _transactionEventsService.IndexCashinEventsForErc20Deposits();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(LykkePayIndexingJob), nameof(Execute), "", ex);
            }
        }
    }
}

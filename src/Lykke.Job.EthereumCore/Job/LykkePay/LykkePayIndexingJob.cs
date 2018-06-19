using Common.Log;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Service.EthereumCore.Core.LykkePay;
using Lykke.Service.EthereumCore.Core.Settings;
using System;
using System.Threading.Tasks;

namespace Lykke.Job.EthereumCore.Job.LykkePay
{
    public class LykkePayIndexingJob
    {
        private readonly ILog _log;
        private readonly IBaseSettings _settings;
        private readonly ILykkePayEventsService _transactionEventsService;

        public LykkePayIndexingJob(ILog log, 
            IBaseSettings settings, 
            ILykkePayEventsService transactionEventsService)
        {
            _transactionEventsService = transactionEventsService;
            _settings = settings;
            _log = log;
        }

        [TimerTrigger("0.00:01:00")]
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

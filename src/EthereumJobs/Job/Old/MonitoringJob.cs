using System;
using System.Threading.Tasks;
using Core.Repositories;
using Common.Log;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;

namespace EthereumJobs.Job
{
    public class MonitoringJob
    {
        private readonly IMonitoringRepository _repository;
        private readonly ILog _logger;

        public MonitoringJob(IMonitoringRepository repository, ILog logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [TimerTrigger("0.00:00:30")]
        public async Task Execute()
        {
            try
            {
                await _repository.SaveAsync(new Monitoring
                {
                    DateTime = DateTime.UtcNow,
                    ServiceName = "EthereumJobService",
                    Version = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion
                });
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync("MonitoringJob", "Execute", "", e ,DateTime.UtcNow);
            }
        }
    }
}

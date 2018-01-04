using System;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Common.Log;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.MonitoringServiceApiCaller;
using System.Reflection;

namespace Lykke.Job.EthereumCore.Job
{
    public class MonitoringJob
    {
        private readonly ILog _logger;
        private readonly MonitoringServiceFacade _monitoringServiceFacade;
        private readonly Version _version;
        private readonly string serviceName = "EthereumCore.EthereumJobs";

        public MonitoringJob(Lykke.MonitoringServiceApiCaller.MonitoringServiceFacade monitoringServiceFacade, ILog logger)
        {
            _monitoringServiceFacade = monitoringServiceFacade;
            _logger = logger;
            _version = Assembly.GetEntryAssembly().GetName().Version;
        }

        [TimerTrigger("0.00:00:30")]
        public async Task Execute()
        {
            try
            {
                await _monitoringServiceFacade.Ping(new Lykke.MonitoringServiceApiCaller.Models.MonitoringObjectPingModel()
                {
                    ServiceName = serviceName,
                    Version = _version.ToString(),
                });
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync("MonitoringJob", "Execute", "", e, DateTime.UtcNow);
            }
        }
    }
}

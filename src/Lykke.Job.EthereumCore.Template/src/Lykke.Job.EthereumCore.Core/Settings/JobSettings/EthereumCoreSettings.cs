namespace Lykke.Job.EthereumCore.Core.Settings.JobSettings
{
        public class EthereumCoreSettings
        {
            public DbSettings Db { get; set; }
            public AzureQueueSettings AzureQueue { get; set; }
            public RabbitMqSettings Rabbit { get; set; }
        }
}
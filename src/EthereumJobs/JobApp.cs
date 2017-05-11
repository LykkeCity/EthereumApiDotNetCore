using System;
using System.Threading.Tasks;
using Core.Settings;
using EthereumJobs.Actions;
using EthereumJobs.Job;
using Microsoft.Extensions.DependencyInjection;
using EthereumJobs.Config;
using Microsoft.Extensions.Configuration;

namespace EthereumJobs
{
    public class JobApp
    {
        public IServiceProvider Services { get; set; }

        public async void Run(IConfigurationRoot configuration)
        {
            var settings = GetSettings(configuration);
            IServiceCollection collection = new ServiceCollection();
            collection.InitJobDependencies(settings);

            Services = collection.BuildServiceProvider();

            // start monitoring
            Services.GetService<MonitoringJob>().Start();

            // restore contract payment events after service shutdown
            await Task.Run(() => Services.GetService<ProcessManualEvents>().Start());
            //await Task.Run(() => Services.GetService<CatchOldUserContractEvents>().Start());

            Console.WriteLine($"----------- All data checked and restored, job is running now {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}-----------");

            RunJobs();
        }

        public void RunJobs()
        {
            //Services.GetService<CheckContractQueueCountJob>().Start();
            //Services.GetService<CheckPaymentsToUserContractsJob>().Start();
            //Services.GetService<RefreshContractQueueJob>().Start();
            // Services.GetService<MonitoringContractBalance>().Start();

            #region OldJobs
            //uncomment
            //Services.GetService<TransferTransactionQueueJob>().Start();
            //Services.GetService<TransferTransactionQueueJob>().Start();
            //Services.GetService<ListenCoinContactsEvents>().Start();
            //Services.GetService<MonitoringCoinTransactionJob>().Start();
            //Services.GetService<PingContractsJob>().Start();

            #endregion

            #region NewJobs
            Services.GetService<MonitoringCoinTransactionJob>().Start();
            Services.GetService<MonitoringTransferContracts>().Start();
            Services.GetService<MonitoringTransferTransactions>().Start();
            Services.GetService<TransferContractPoolJob>().Start();
            Services.GetService<TransferContractUserAssignmentJob>().Start();

            #endregion
        }

        static IBaseSettings GetSettings(IConfigurationRoot configuration)
        {
            var connectionString = configuration.GetConnectionString("ConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("Please, provide connection string");
            }

            var settings = GeneralSettingsReader.ReadGeneralSettings<SettingsWrapper>(connectionString);

            return settings.EthereumCore;
        }
    }
}

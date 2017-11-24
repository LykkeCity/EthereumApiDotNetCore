using System;
using System.Threading.Tasks;
using Core.Settings;
using EthereumJobs.Job;
using Microsoft.Extensions.DependencyInjection;
using EthereumJobs.Config;
using Microsoft.Extensions.Configuration;
using Lykke.JobTriggers.Triggers;
using System.Reflection;
using System.Threading;
using System.Runtime.Loader;
using Lykke.JobTriggers.Extenstions;
using Services;

namespace EthereumJobs
{
    public class JobApp
    {
        public IServiceProvider Services { get; set; }

        public async void Run(IConfigurationRoot configuration)
        {
            var settings = GetSettings(configuration);
            IServiceCollection collection = new ServiceCollection();

            collection.InitJobDependencies(settings.EthereumCore, settings.SlackNotifications);
            collection.AddSingleton(settings);
            collection.AddTriggers(pool =>
            {
                // default connection must be initialized
                pool.AddDefaultConnection(settings.EthereumCore.Db.DataConnString);
                //// you can add additional connection strings and then specify it in QueueTriggerAttribute 
                //pool.AddConnection("custom", additionalConnectionString);
            });

            Services = collection.BuildServiceProvider();
            Services.ActivateRequestInterceptor();
            // restore contract payment events after service shutdown
            //await Task.Run(() => Services.GetService<ProcessManualEvents>().Start());
            //await Task.Run(() => Services.GetService<CatchOldUserContractEvents>().Start());

            Console.WriteLine($"----------- Job is running now {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}-----------");

            RunJobs();
        }

        public void RunJobs()
        {
            var triggerHost = new TriggerHost(Services);

            triggerHost.ProvideAssembly(GetType().GetTypeInfo().Assembly);

            var end = new ManualResetEvent(false);

            AssemblyLoadContext.Default.Unloading += ctx =>
            {
                Console.WriteLine("SIGTERM recieved");
                triggerHost.Cancel();

                end.WaitOne();
            };

            triggerHost.Start().Wait();
            end.Set();
        }

        static SettingsWrapper GetSettings(IConfigurationRoot configuration)
        {
            var connectionString = configuration.GetConnectionString("ConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("Please, provide connection string");
            }

            var settings = GeneralSettingsReader.ReadGeneralSettings<SettingsWrapper>(connectionString);

            return settings;
        }
    }
}

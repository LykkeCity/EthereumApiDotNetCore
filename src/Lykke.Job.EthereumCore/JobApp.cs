using System;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Job.EthereumCore.Job;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Job.EthereumCore.Config;
using Microsoft.Extensions.Configuration;
using Lykke.JobTriggers.Triggers;
using System.Reflection;
using System.Threading;
using System.Runtime.Loader;
using Lykke.JobTriggers.Extenstions;
using Lykke.Service.EthereumCore.Services;

namespace Lykke.Job.EthereumCore
{
    //public class JobApp
    //{
    //    public IServiceProvider Services { get; set; }

    //    public async void Run(IConfigurationRoot configuration)
    //    {
    //        var settings = GetSettings(configuration);
    //        IServiceCollection collection = new ServiceCollection();

    //        collection.InitJobDependencies(settings.EthereumCore, settings.SlackNotifications);
    //        collection.AddSingleton(settings);
    //        collection.AddTriggers(pool =>
    //        {
    //            // default connection must be initialized
    //            pool.AddDefaultConnection(settings.EthereumCore.Db.DataConnString);
    //        });

    //        Services = collection.BuildServiceProvider();
    //        Services.ActivateRequestInterceptor();

    //        Console.WriteLine($"----------- Job is running now {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}-----------");

    //        RunJobs();
    //    }

    //    public void RunJobs()
    //    {
    //        var triggerHost = new TriggerHost(Services);

    //        triggerHost.ProvideAssembly(GetType().GetTypeInfo().Assembly);

    //        var end = new ManualResetEvent(false);

    //        AssemblyLoadContext.Default.Unloading += ctx =>
    //        {
    //            Console.WriteLine("SIGTERM recieved");
    //            triggerHost.Cancel();

    //            end.WaitOne();
    //        };

    //        triggerHost.Start().Wait();
    //        end.Set();
    //    }

    //    static AppSettings GetSettings(IConfigurationRoot configuration)
    //    {
    //        var connectionString = configuration.GetConnectionString("ConnectionString");
    //        if (string.IsNullOrEmpty(connectionString))
    //        {
    //            throw new Exception("Please, provide connection string");
    //        }

    //        var settings = GeneralSettingsReader.ReadGeneralSettings<AppSettings>(connectionString);

    //        return settings;
    //    }
    //}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Job.EthereumCore;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Job.EthereumCore.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.AspNetCore.Hosting;

namespace JobRunner
{
    public class Program
    {
        public static string EnvInfo => Environment.GetEnvironmentVariable("ENV_INFO");

        public static async Task Main(string[] args)
        {
            Console.WriteLine($"{PlatformServices.Default.Application.ApplicationName} version {PlatformServices.Default.Application.ApplicationVersion}");
#if DEBUG
            Console.WriteLine("Is DEBUG");
#else
            Console.WriteLine("Is RELEASE");
#endif
            Console.WriteLine($"ENV_INFO: {EnvInfo}");

            try
            {
                var webHost = new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls("http://*:5000")
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>()
                    .UseApplicationInsights()
                    .Build();

                await webHost.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error:");
                Console.WriteLine(ex);

                // Lets devops to see startup error in console between restarts in the Kubernetes
                var delay = TimeSpan.FromMinutes(1);

                Console.WriteLine();
                Console.WriteLine($"Process will be terminated in {delay}. Press any key to terminate immediately.");

                await Task.WhenAny(
                            Task.Delay(delay),
                            Task.Run(() =>
                            {
                                Console.ReadKey(true);
                            }));
            }

            Console.WriteLine("Terminated");
        }


        //public static void Main(string[] args)
        //{
        //    Console.Clear();
        //    Console.Title = "Ethereum Lykke.Service.EthereumCore.Core Job - Ver. " + Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion;

        //    try
        //    {
        //        FileInfo fi = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
        //        var location = Path.Combine(fi.DirectoryName, "..", "..", "..");
        //        var builder = new ConfigurationBuilder()
        //            .SetBasePath(location)
        //            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        //            .AddEnvironmentVariables();
        //        var configuration = builder.Build();
                
        //        var app = new JobApp();
        //        app.Run(configuration);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("cannot start jobs! Exception: " + e.Message);
        //        Console.WriteLine("Press any key to exit...");
        //        Console.ReadKey();
        //        return;
        //    }

        //    Console.WriteLine("Web job started");
        //    Console.WriteLine("Utc time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

        //    Console.WriteLine("Press 'q' to quit.");

        //    while (Console.ReadLine() != "q") continue;
        //}
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Job.EthereumCore;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Job.EthereumCore.Config;
using Microsoft.Extensions.Configuration;

namespace JobRunner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Clear();
            Console.Title = "Ethereum Lykke.Service.EthereumCore.Core Job - Ver. " + Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion;

            try
            {
                FileInfo fi = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
                var location = Path.Combine(fi.DirectoryName, "..", "..", "..");
                var builder = new ConfigurationBuilder()
                    .SetBasePath(location)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();
                var configuration = builder.Build();
                
                var app = new JobApp();
                app.Run(configuration);
            }
            catch (Exception e)
            {
                Console.WriteLine("cannot start jobs! Exception: " + e.Message);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Web job started");
            Console.WriteLine("Utc time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            Console.WriteLine("Press 'q' to quit.");

            while (Console.ReadLine() != "q") continue;
        }
    }
}

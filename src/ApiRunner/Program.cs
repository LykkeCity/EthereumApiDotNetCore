using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Settings;
using EthereumApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.RPC.TransactionManagers;

namespace ApiRunner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "Ethereum Self-hosted API - Ver. " + Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion;

            var host = new WebHostBuilder()
               .UseKestrel()
               .UseContentRoot(Directory.GetCurrentDirectory())
               .UseIISIntegration()
               .UseStartup<Startup>()
               .UseApplicationInsights()
               .Build();

            Console.WriteLine($"Web Server is running ");
            Console.WriteLine("Utc time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            host.Run();
        }
    }
}

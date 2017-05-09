using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Settings;
using EthereumApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ApiRunner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var arguments = args.Select(t => t.Split('=')).ToDictionary(spl => spl[0].Trim('-'), spl => spl[1]);

            if (!arguments.ContainsKey("port"))
            {
                Console.WriteLine("Please, specify command line parameters:");
                Console.WriteLine("-port=<port> # port for web server");
                Console.ReadLine();
                return;
            }

            Console.Clear();
            Console.Title = "Ethereum Self-hosted API - Ver. " + Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion;

            var url = $"http://*:{arguments["port"]}";

            var host = new WebHostBuilder()
               .UseKestrel()
               .UseUrls(url)
               .UseContentRoot(Directory.GetCurrentDirectory())
               .UseIISIntegration()
               .UseStartup<Startup>()
               .Build();

            Console.WriteLine($"Web Server is running - {url}");
            Console.WriteLine("Utc time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            host.Run();
        }

        static string ReadSettingsFile()
        {
            return File.ReadAllText("generalsettings.json");
        }


        static void CheckSettings(BaseSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.EthereumMainAccount))
                throw new Exception("EthereumMainAccount is missing");

            if (string.IsNullOrWhiteSpace(settings.EthereumMainAccountPassword))
                throw new Exception("EthereumMainAccountPassword is missing");

            if (string.IsNullOrWhiteSpace(settings.EthereumUrl))
                throw new Exception("EthereumUrl is missing");

            if (string.IsNullOrWhiteSpace(settings.Db?.DataConnString))
                throw new Exception("DataConnString is missing");

            if (string.IsNullOrWhiteSpace(settings.Db?.LogsConnString))
                throw new Exception("LogsConnString is missing");

            if (string.IsNullOrWhiteSpace(settings.Db?.DictsConnString))
                throw new Exception("DictsConnString is missing");

            if (string.IsNullOrWhiteSpace(settings.Db?.SharedConnString))
                throw new Exception("SharedConnString is missing");

            if (string.IsNullOrWhiteSpace(settings.Db?.SharedTransactionConnString))
                throw new Exception("SharedTransactionConnString is missing");

            if (string.IsNullOrWhiteSpace(settings.MainExchangeContract?.Abi))
                throw new Exception("MainExchangeContract.Abi is missing");

            if (string.IsNullOrWhiteSpace(settings.MainExchangeContract?.Address))
                throw new Exception("MainExchangeContract.Address is missing");
        }
    }
}

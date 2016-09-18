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

			Console.Clear();
			Console.Title = "Ethereum Self-hosted API - Ver. " + Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion;

			var settings = GetSettings(arguments);
			if (settings == null)
			{
				Console.WriteLine("Press any key to exit...");
				Console.ReadKey();
				return;
			}
			
			try
			{
				CheckSettings(settings);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				Console.WriteLine("Press any key to exit...");
				Console.ReadKey();
				return;
			}

			var url = $"http://*:{arguments["port"]}";

			var host = new WebHostBuilder()
			   .UseKestrel()
			   .UseUrls(url)
			   .UseContentRoot(Directory.GetCurrentDirectory())
			   .UseIISIntegration()
			   .ConfigureServices(collection => collection.AddSingleton<IBaseSettings>(settings))
			   .UseStartup<Startup>()
			   .Build();

			Console.WriteLine($"Web Server is running - {url}");
			Console.WriteLine("Utc time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

			host.Run();
		}

		static BaseSettings GetSettings(Dictionary<string, string> arguments)
		{
			var settingsData = ReadSettingsFile();

			if (string.IsNullOrWhiteSpace(settingsData))
			{
				Console.WriteLine("Please, provide generalsettings.json file");
				return null;
			}


			if (!arguments.ContainsKey("port"))
			{
				Console.WriteLine("Please, specify command line parameters:");
				Console.WriteLine("-port=<port> # port for web server");
				return null;
			}

			var settings = GeneralSettingsReader.ReadSettingsFromData<BaseSettings>(settingsData);

			return settings;
		}


		static string ReadSettingsFile()
		{
#if DEBUG
			return File.ReadAllText(@"..\..\settings\generalsettings.json");
#else
			return File.ReadAllText("generalsettings.json");
#endif
		}


		static void CheckSettings(BaseSettings settings)
		{
			if (string.IsNullOrWhiteSpace(settings.EthereumMainAccount))
				throw new Exception("EthereumMainAccount is missing");
			if (string.IsNullOrWhiteSpace(settings.EthereumMainAccountPassword))
				throw new Exception("EthereumMainAccountPassword is missing");
			if (string.IsNullOrWhiteSpace(settings.EthereumMainContractAddress))
				throw new Exception("EthereumMainContractAddress is missing");
			if (string.IsNullOrWhiteSpace(settings.EthereumPrivateAccount))
				throw new Exception("EthereumPrivateAccount is missing");
			if (string.IsNullOrWhiteSpace(settings.EthereumUrl))
				throw new Exception("EthereumUrl is missing");

			if (string.IsNullOrWhiteSpace(settings.Db?.DataConnString))
				throw new Exception("DataConnString is missing");
			if (string.IsNullOrWhiteSpace(settings.Db?.LogsConnString))
				throw new Exception("LogsConnString is missing");
			if (string.IsNullOrWhiteSpace(settings.Db?.ExchangeQueueConnString))
				throw new Exception("ExchangeQueueConnString is missing");
			if (string.IsNullOrWhiteSpace(settings.Db?.EthereumNotificationsConnString))
				throw new Exception("EthereumNotificationsConnString is missing");

			if (string.IsNullOrWhiteSpace(settings.MainContract?.Abi))
				throw new Exception("MainContract abi is invalid");
			if (string.IsNullOrWhiteSpace(settings.MainContract?.ByteCode))
				throw new Exception("MainContract bytecode is invalid");

			if (string.IsNullOrWhiteSpace(settings.UserContract?.Abi))
				throw new Exception("UserContract abi is invalid");
			if (string.IsNullOrWhiteSpace(settings.UserContract?.ByteCode))
				throw new Exception("UserContract bytecode is invalid");
		}
	}
}

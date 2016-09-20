using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure.Blob;
using Core.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Services;

namespace ContractBuilder
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var exit = false;

			var configurationBuilder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json");
			var configuration = configurationBuilder.Build();

			while (!exit)
			{
				Console.WriteLine("Choose number: ");
				//Console.WriteLine("1. Deploy main contract from local json file");
				Console.WriteLine("2. Transfer contracts");
				Console.WriteLine("0. Exit");

				var input = Console.ReadLine();

				switch (input)
				{
					//case "1":
					//	DeployMainContractLocal().Wait();
					//	break;
					case "2":
						new ContractTransferJob().Start(configuration["WalletConnString"], configuration["EthConnString"]).Wait();
						break;
					case "0":
						exit = true;
						break;
					default:
						Console.WriteLine("Bad input!");
						continue;
				}

				Console.WriteLine("Done!");
			}
		}

		static void UpdateMainContract()
		{
			var contractAbi = GetFileContent("MainContract.abi");
			var contractByteCode = GetFileContent("MainContract.bin");

			var settings = GetCurrentSettings();

			settings.MainContract.Abi = contractAbi;
			settings.MainContract.ByteCode = contractByteCode;

			SaveSettings(settings);
		}

		static void UpdateUserContract()
		{
			var contractAbi = GetFileContent("UserContract.abi");
			var contractByteCode = GetFileContent("UserContract.bin");

			var settings = GetCurrentSettings();

			settings.UserContract.Abi = contractAbi;
			settings.UserContract.ByteCode = contractByteCode;

			SaveSettings(settings);
		}


		static async Task DeployMainContractLocal()
		{
			Console.WriteLine("Begin contract deployment process");
			try
			{
				var json = File.ReadAllText("generalsettings.json");
				var settings = JsonConvert.DeserializeObject<BaseSettings>(json);
				string contractAddress = await new ContractService(settings, null).GenerateMainContract();

				settings.EthereumMainContractAddress = contractAddress;
				Console.WriteLine("New contract: " + contractAddress);

				File.WriteAllText("generalsettings.json", JsonConvert.SerializeObject(settings, Formatting.Indented));

				Console.WriteLine("Contract address stored in generalsettings.json file");
			}
			catch (Exception e)
			{
				Console.WriteLine("Action failed!");
				Console.WriteLine(e.Message);
			}
		}

		static BaseSettings GetCurrentSettings()
		{
			var json = File.ReadAllText("generalsettings.json");
			var settings = JsonConvert.DeserializeObject<BaseSettings>(json);
			return settings;
		}

		static void SaveSettings(BaseSettings settings)
		{
			File.WriteAllText("generalsettings.json", JsonConvert.SerializeObject(settings, Formatting.Indented));
		}


		static string GetFileContent(string fileName)
		{
			return File.ReadAllText(Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "contracts", "bin", fileName));
		}
	}
}
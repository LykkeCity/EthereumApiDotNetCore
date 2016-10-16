using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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
				Console.WriteLine("2. Deploy main exchange contract using local json file");
				Console.WriteLine("3. Deploy coin contract using local json file");
				Console.WriteLine("0. Exit");

				var input = Console.ReadLine();

				switch (input)
				{
					//case "1":
					//	DeployMainContractLocal().Wait();
					//	break;
					case "2":
						DeployMainExchangeContract().Wait();
						break;
					case "3":
						DeployCoinContract().Wait();
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

		static async Task DeployMainContractLocal()
		{
			Console.WriteLine("Begin contract deployment process");
			try
			{
				var settings = GetCurrentSettings();

				string contractAddress = await new ContractService(settings, null).CreateContract(settings.MainContract.Abi, settings.MainContract.ByteCode);

				settings.EthereumMainContractAddress = contractAddress;
				Console.WriteLine("New contract: " + contractAddress);

				SaveSettings(settings);

				Console.WriteLine("Contract address stored in generalsettings.json file");
			}
			catch (Exception e)
			{
				Console.WriteLine("Action failed!");
				Console.WriteLine(e.Message);
			}
		}

		static async Task DeployCoinContract()
		{
			string name, path, multi;
			BigInteger multiInt;
			do
			{
				Console.WriteLine("Enter coin name:");
				name = Console.ReadLine();
			} while (string.IsNullOrWhiteSpace(name));
			do
			{
				Console.WriteLine("Enter coin file name:");
				path = Console.ReadLine();
			} while (string.IsNullOrWhiteSpace(path) || !File.Exists(GetFilePath(path + ".abi")));
			do
			{
				Console.WriteLine("Enter coin multiplier:");
				multi = Console.ReadLine();
			} while (string.IsNullOrWhiteSpace(multi) || !BigInteger.TryParse(multi, out multiInt));

			Console.WriteLine("Begin coin contract deployment process");
			try
			{
				var abi = GetFileContent(path + ".abi");
				var bytecode = GetFileContent(path + ".bin");
				var settings = GetCurrentSettings();
				string contractAddress = await new ContractService(settings, null).CreateContract(abi, bytecode, settings.EthereumMainExchangeContractAddress);
				if (settings.CoinContracts == null)
					settings.CoinContracts = new Dictionary<string, EthereumContract>();
				settings.CoinContracts.Add(contractAddress, new EthereumContract { Name = name, Abi = abi, Multiplier = multi });

				Console.WriteLine("New coin contract: " + contractAddress);

				SaveSettings(settings);

				Console.WriteLine("Contract address stored in generalsettings.json file");
			}
			catch (Exception e)
			{
				Console.WriteLine("Action failed!");
				Console.WriteLine(e.Message);
			}
		}

		static async Task DeployMainExchangeContract()
		{
			Console.WriteLine("Begin main exchange contract deployment process");
			try
			{
				var settings = GetCurrentSettings();
				var abi = GetFileContent("MainExchange.abi");
				var bytecode = GetFileContent("MainExchange.bin");
				string contractAddress = await new ContractService(settings, null).CreateContract(abi, bytecode);

				settings.MainExchangeContract = new EthereumContract { Abi = abi, ByteCode = bytecode };
				settings.EthereumMainExchangeContractAddress = contractAddress;
				Console.WriteLine("New main exchange contract: " + contractAddress);

				SaveSettings(settings);

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
			var json = File.ReadAllText(GetSettingsPath());
			var settings = JsonConvert.DeserializeObject<BaseSettings>(json);
			return settings;
		}

		static void SaveSettings(BaseSettings settings)
		{
			File.WriteAllText(GetSettingsPath(), JsonConvert.SerializeObject(settings, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
		}

		static string GetFilePath(string fileName)
		{
#if DEBUG
			return Path.Combine("contracts", "bin", fileName);
#else
			return Path.Combine("contracts", "bin", fileName);
#endif
		}

		static string GetFileContent(string fileName)
		{
			return File.ReadAllText(GetFilePath(fileName));
		}

		static string GetSettingsPath()
		{
#if DEBUG
			return "..\\..\\settings\\generalsettings.json";
#else
			return "generalsettings.json";
#endif
		}
	}
}
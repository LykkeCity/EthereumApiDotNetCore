using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Log;
using Core.Settings;
using EthereumJobs.Config;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Services;

namespace Tests
{
	[SetUpFixture]
	public class Config
	{
		public static IServiceProvider Services { get; set; }
		public static ILog Logger => Services.GetService<ILog>();

		private IBaseSettings ReadSettings()
		{
			try
			{
				var json = File.ReadAllText(@"..\settings\generalsettings.json");
				if (string.IsNullOrWhiteSpace(json))
				{

					return null;
				}
				BaseSettings settings = GeneralSettingsReader.ReadSettingsFromData<BaseSettings>(json);

				return settings;
			}
			catch (Exception e)
			{
				return null;
			}
		}


		[OneTimeSetUp]
		public void Initialize()
		{
			Constants.StoragePrefix = "tests";

			IServiceCollection collection = new ServiceCollection();
			var settings = ReadSettings();

			Assert.NotNull(settings, "Please, provide generalsettings.json file");

			collection.InitJobDependencies(settings);

			Services = collection.BuildServiceProvider();

			Assert.DoesNotThrowAsync(() => Services.GetService<IContractService>().GetCurrentBlock(), "Please, run ethereum node (geth.exe)");
		}
	}
}

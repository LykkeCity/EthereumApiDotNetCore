using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Repositories;
using Core.Settings;
using EthereumJobs.Config;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Services;
using Common.Log;

// ReSharper disable once CheckNamespace
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

        private TestSettings ReadTestSettings()
        {
            try
            {
                var json = File.ReadAllText(@"..\settings\testsettings.json");
                if (string.IsNullOrWhiteSpace(json))
                {

                    return null;
                }
                TestSettings settings = GeneralSettingsReader.ReadSettingsFromData<TestSettings>(json);

                return settings;
            }
            catch (Exception e)
            {
                return null;
            }
        }


        [OneTimeSetUp]
        public async Task Initialize()
        {
            Constants.StoragePrefix = "tests";

            IServiceCollection collection = new ServiceCollection();
            var settings = ReadSettings();

            var testSetting = ReadTestSettings();

            Assert.NotNull(settings, "Please, provide generalsettings.json file");
            Assert.NotNull(testSetting, "Please, provide testsettings.json file");

            collection.InitJobDependencies(settings);

            Services = collection.BuildServiceProvider();


            var coinRepo = Services.GetService<ICoinRepository>();
            foreach (var item in settings.CoinContracts)
                await coinRepo.InsertOrReplace(new Coin
                {
                    AdapterAddress = item.Value.Address,
                    Id = item.Key,
                    Multiplier = testSetting.CoinContracts[item.Key].Multiplier,
                    BlockchainDepositEnabled = testSetting.CoinContracts[item.Key].Payable,
                    Blockchain = "ethereum"
                });


            //Assert.DoesNotThrowAsync(() => Services.GetService<IContractService>().GetCurrentBlock(), "Please, run ethereum node (geth.exe)");
        }

        public class TestSettings
        {
            public Dictionary<string, TestContract> CoinContracts { get; set; }
        }

        public class TestContract : Core.Settings.EthereumContract
        {
            public int Multiplier { get; set; }
            public bool Payable { get; set; }
        }
    }
}

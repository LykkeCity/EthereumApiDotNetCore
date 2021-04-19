using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common.Log;
using Lykke.Service.EthereumCore.Services;

// ReSharper disable once CheckNamespace
namespace Tests
{
    public class Config
    {
        public static IServiceProvider Services { get; set; }
        public static ILog Logger => Services.GetService<ILog>();

        private AppSettings ReadSettings()
        {
            try
            {
                var url = File.ReadAllText(@"..\..\..\configurationUrl.url");
                if (string.IsNullOrWhiteSpace(url))
                {

                    return null;
                }

                AppSettings settings = GeneralSettingsReader.ReadGeneralSettings<AppSettings>(url);

                return settings;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        //private TestSettings ReadTestSettings()
        //{
        //    try
        //    {
        //        var json = File.ReadAllText(@"..\..\..\generalsettings.json");
        //        if (string.IsNullOrWhiteSpace(json))
        //        {

        //            return null;
        //        }
        //        TestSettings settings = GeneralSettingsReader.ReadSettingsFromData<TestSettings>(json);

        //        return settings;
        //    }
        //    catch (Exception e)
        //    {
        //        return null;
        //    }
        //}

        public async Task Initialize()
        {
            //Constants.StoragePrefix = "tests";

            IServiceCollection collection = new ServiceCollection();
            var settings = ReadSettings();

            //var testSetting = ReadTestSettings();
            Assert.IsNotNull(settings, "Please, provide generalsettings.json file");

            //TODO:Fix
            //collection.InitJobDependencies(settings.EthereumCore, settings.SlackNotifications);

            Services = collection.BuildServiceProvider();
            Services.ActivateRequestInterceptor();
        }

        public class TestSettings
        {
            public Dictionary<string, TestContract> CoinContracts { get; set; }
        }

        public class TestContract : Lykke.Service.EthereumCore.Core.Settings.EthereumContract
        {
            public int Multiplier { get; set; }
            public bool Payable { get; set; }
        }
    }
}
